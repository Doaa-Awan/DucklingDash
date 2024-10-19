using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using GameLibrary;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.System.Diagnostics.Telemetry;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Storage;
using System.IO;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage.Provider;

/* UWP Game Template
 * Created By: Melissa VanderLely
 * Modified By: Doaa Awan
 */

namespace GameInterface
{
    public sealed partial class MainPage : Page
    {
        private struct PlayerState
        {
            public Thickness Position;
            public double RotationAngle;
        }

        #region Game Pieces

        private static GamePiece player;
        private static GamePiece dot;
        private static GamePiece bonusDot;
        private static GamePiece babyDuck;
        private static GamePiece enemy;

        #endregion

        #region Collections

        private static List<GamePiece> collectedDucks = new List<GamePiece>(); //list of baby ducks
        private List<PlayerState> playerStates = new List<PlayerState>(); //list of player positions and angles
        List<int> scores = new List<int>(); //list to hold scores

        #endregion

        #region Objects

        private Random random = new Random(); //random number generator
        private DispatcherTimer timer = new DispatcherTimer(); //timer

        //key press
        private Windows.System.VirtualKey currentDirection; //current key press direction
        private Windows.System.VirtualKey lastDirection; //last key press direction

        //sounds
        MediaPlayer chirp = new MediaPlayer(); //chirp sound
        MediaPlayer quack = new MediaPlayer(); //quack sound

        //border outline
        SolidColorBrush highlightBrush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(0xFF, 0x6D, 0xC9, 0xEF));
        SolidColorBrush defaultBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

        //local file
        StorageFile localFile = null;
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        #endregion

        #region Defaults

        private int score = 0;
        private int highscore = 0;
        private int speed = 10;
        private int defaultSpeed = 10;

        //change settings for level up
        private string message = "";
        private int setScore = 0;
        private int setSpeed = 10;

        //score file name
        string fileName = "scores.txt";

        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            //event handlers
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            timer.Tick += HandleMovement;

            ReadFile();
            txtScores.Text = "High Scores\n\n";

            //set overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;
            btnScores.Visibility = Visibility.Visible;

            //button borders
            btnRestart.BorderBrush = highlightBrush;
            btnQuit.BorderBrush = defaultBrush;

            InitializeMediaPlayer();
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (gridRestart.Visibility == Visibility.Visible) //if restart menu is visible
            {
                //if button is highlighted and enter key is pressed

                if (((SolidColorBrush)btnRestart.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Enter)
                {
                    Restart();
                }
                else if (((SolidColorBrush)btnQuit.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Enter)
                {
                    Quit();
                }

                //if button is highlighted and left or right key is pressed

                if (((SolidColorBrush)btnRestart.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Left)
                {
                    btnQuit.BorderBrush = highlightBrush;
                    btnRestart.BorderBrush = defaultBrush;
                }
                else if (((SolidColorBrush)btnQuit.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Right)
                {
                    btnRestart.BorderBrush = highlightBrush;
                    btnQuit.BorderBrush = defaultBrush;
                }
            }

            if (lblOverlay.Visibility == Visibility.Visible) //if overlay is visible
            {
                if (e.VirtualKey == Windows.System.VirtualKey.Space) //space bar pressed
                    StartGame();
            }
            //if overlay is collapsed or restart menu is collapsed
            else if (lblOverlay.Visibility == Visibility.Collapsed || gridRestart.Visibility == Visibility.Collapsed)
            {
              //change direction based on key press
                switch (e.VirtualKey)
                {
                    case Windows.System.VirtualKey.W:
                        currentDirection = Windows.System.VirtualKey.Up;
                        break;
                    case Windows.System.VirtualKey.A:
                        currentDirection = Windows.System.VirtualKey.Left;
                        break;
                    case Windows.System.VirtualKey.S:
                        currentDirection = Windows.System.VirtualKey.Down;
                        break;
                    case Windows.System.VirtualKey.D:
                        currentDirection = Windows.System.VirtualKey.Right;
                        break;
                    case Windows.System.VirtualKey.Space:                         
                        currentDirection = lastDirection;
                        break;
                    default:
                        currentDirection = e.VirtualKey;
                        break;
                }
            }
        }

        private void StartGame()
        {
            //disable overlay
            lblOverlay.Visibility = Visibility.Collapsed;
            screenOverlay.Visibility = Visibility.Collapsed;
            btnScores.Visibility = Visibility.Collapsed;
            gridScores.Visibility = Visibility.Collapsed;

            //set current direction to right
            currentDirection = Windows.System.VirtualKey.Right;

            //display score
            txtScore.Text = score.ToString();

            //create initial player and dot pieces
            player = CreatePiece("player", 80, -550, -510);  // img, size, left, top
            dot = CreatePiece("dot", 15, 210, -300);

            //add initial player location and angle to list
            playerStates.Add(new PlayerState { Position = player.Location, RotationAngle = 0 });

            //set initial speed of player movement based on timer
            timer.Interval = TimeSpan.FromMilliseconds(speed);

            //display speed setting based on score
            CheckSpeed();

            //start timer
            timer.Start();
        }

        private void HandleMovement(object sender, object e)
        {
            //move player continuously in current direction
            player.Move(currentDirection, 20);

            //if player collides with dot, collect dot
            if (CollisionDetected(player, dot))
            {
                HandleDotCollection();
            }

            //if bonus points fish exists
            if (bonusDot != null)
            {
                MovePieceRandomly(bonusDot, 5); //keep fish moving
            }
            
            //if bonus points fish exists and player collects it
            if (bonusDot != null && CollisionDetected(player, bonusDot))
            {
                HandleBonusDotCollection();
                lblInfo.Text = "";
            }

            //if enemy exists
            if (enemy != null)
            {
                MovePieceRandomly(enemy, 5); //keep enemy moving
            }

            //if enemy exists and player hits it
            if (enemy != null && CollisionDetected(player, enemy))
            {
                Crash();
            }

            //if fish does not exist and score hits certain points, generate fish
            if (bonusDot == null && (score == 10 || score == 35 || score == 45))
            {
                bonusDot = CreatePiece("fish", 40, 0, 0, true);
                bonusDot.PieceImg.Opacity = 0.50;
                lblInfo.Text = "eat the fish";
            }

            //if enemy does not exist and score hits 15
            if (enemy == null && (score == 20 || score == 60))
            {
                enemy = CreatePiece("turtle", 80, 0, 0, true);
                enemy.PieceImg.Opacity = 0.50;
                lblInfo.Text = "do not eat the turtle";
            }

            //remove turtle when score is at 40
            if (enemy != null && score == 40)
            {
                gridMain.Children.Remove(enemy.PieceImg);
                enemy = null;
            }

            AlignBabyDucks(); //ensure baby ducks follow directly behind player
            CheckCrash(); //check if player hits border or baby ducks
        }

        private void Crash()
        {
            quack.Play();
            message = "";
            setScore = 0;
            setSpeed = defaultSpeed;
            ResetGame();
            gridRestart.Visibility = Visibility.Visible;
        }

        //reset game
        private async void ResetGame()
        {
            timer.Stop(); //stop timer
            currentDirection = Windows.System.VirtualKey.None; //reset direction

            //remove all game pieces from the screen
            gridMain.Children.Remove(player.PieceImg);
            gridMain.Children.Remove(dot.PieceImg);

            if (bonusDot != null) //if bonusDot exists, remove it
            {
                gridMain.Children.Remove(bonusDot.PieceImg);
                bonusDot = null;
            }

            if (enemy != null) //if enemy exists, remove it
            {
                gridMain.Children.Remove(enemy.PieceImg);
                enemy = null;
            }

            foreach (var babyDuck in collectedDucks) //remove all baby ducks
            {
                gridMain.Children.Remove(babyDuck.PieceImg);
            }

            if (score > highscore)
            {
                _ = await new MessageDialog("New Highscore!").ShowAsync();
            }

            AddScore();

            score = setScore; //set new score
            speed = setSpeed; //set new speed

            lblOverlay.Text = $"{message}Press Space to start".ToUpper();
            lblInfo.Text = ""; //clear text

            //clear lists
            collectedDucks.Clear();
            playerStates.Clear();
        }

        #region High Score

        //Read high scores text file and store values
        private async void ReadFile()
        {
            StorageFile readFrom;

            try
            {
                //check if file exists in local folder
                localFile = await localFolder.GetFileAsync(fileName);

                //uncomment this to empty contents of local file
                //await FileIO.WriteTextAsync(localFile, string.Empty);

                readFrom = localFile;
            }
            catch //file doesn't exist in local folder
            {
                //get file from assets folder
                StorageFolder appInstalled = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assetsFolder = await appInstalled.GetFolderAsync("Assets");

                //get the file from the assets folder
                StorageFile storageFile = await assetsFolder.GetFileAsync(fileName);

                readFrom = storageFile;
            }

            //reset title 
            txtScores.Text = "High Scores\n\n";

            //read file
            IList<string> fileLines = await FileIO.ReadLinesAsync(readFrom);

            //read each line and add to list of scores if the score is not 0
            foreach (string line in fileLines)
            {
                int num = Int32.Parse(line);
                if (num != 0)
                {
                    scores.Add(num); //add to collection
                    txtScores.Text += $"{num}\n";
                }
            }

            SetHighscore(); //update highscore text box
        }

        private void AddScore() 
        {
            //if score is not 0 and does not already exist, add to collection
            if (score != 0 && !scores.Contains(score))
            {
                scores.Add(score);
            }

            //sort scores by highest first
            scores.Sort((a, b) => b.CompareTo(a));

            //keep top 10 scores
            if (scores.Count > 10)
            {
                scores.RemoveRange(10, scores.Count - 10); 
            }

            //reset title
            txtScores.Text = "High Scores\n\n";

            //display scores in collection
            foreach (int num in scores)
            {
                txtScores.Text += $"{num}\n";
            }
        }

        private async Task OverwriteScoresAsync()
        {
            try
            {
                //check if local file exists
                localFile = await localFolder.GetFileAsync(fileName);
            }
            catch (FileNotFoundException)
            {
                //if file does not exist, create it from copying text file from assets folder
                StorageFolder appInstalled = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assetsFolder = await appInstalled.GetFolderAsync("Assets");

                //get txtScores file from assets folder
                StorageFile storageFile = await assetsFolder.GetFileAsync(fileName);

                //copy text file to local folder
                localFile = await storageFile.CopyAsync(localFolder, fileName, NameCollisionOption.ReplaceExisting);
            }

            CachedFileManager.DeferUpdates(localFile);

            //overwrite local file with scores from collection
            await FileIO.WriteTextAsync(localFile, string.Join("\n", scores));

            await CachedFileManager.CompleteUpdatesAsync(localFile);
        }


        private void SetHighscore()
        {
            //find highest score from collection of scores
            if (scores.Count > 0)
            {
                foreach (int num in scores)
                {
                    if (num >= highscore)
                    {
                        highscore = num;
                    }
                }
                txtHighscore.Text = highscore.ToString(); 
            }
        }

        #endregion

        #region Collect Points

        private void HandleBonusDotCollection() //bonus points fish collected
        {
            score += 2; //2 bonus points
            txtScore.Text = score.ToString(); //update score

            if (score > highscore)
                txtHighscore.Text = score.ToString();

            gridMain.Children.Remove(bonusDot.PieceImg); //remove piece
            bonusDot = null;

            CheckLevelUp(); //check if level up
        }

        private void HandleDotCollection() //dot collected
        {
            score++; //increment score
            txtScore.Text = score.ToString(); //display score

            if (score > highscore)
                txtHighscore.Text = score.ToString();

            //add baby duck
            babyDuck = CreatePiece("player", 40, 700, 700);
            collectedDucks.Add(babyDuck);

            chirp.Play();

            SpawnNewDot(); //remove dot and spawn new one
            CheckLevelUp(); //check if level up
        }

        private void SpawnNewDot() //remove dot collected and generate new one
        {
            int gridSize = 620;
            int randomNum = random.Next(0, gridSize - 30);
            gridMain.Children.Remove(dot.PieceImg);
            dot = CreatePiece("dot", 15, randomNum, randomNum);
        }

        #endregion 

        #region Collision

        private async void CheckCrash()
        {
            int ignoreCount = 3; //ducks immediately behind player to ignore
            int playerLeft = (int)player.Location.Left;
            int playerTop = (int)player.Location.Top;
            RotateTransform rotateTransform = player.RotateTransform;

            //start checking for collision from the baby duck at index 'ignoreCount'
            for (int i = ignoreCount; i < collectedDucks.Count; i++)
            {
                GamePiece babyDuck = collectedDucks[i];
                if (CollisionDetected(player, babyDuck))
                {
                    Crash();
                    break;
                }
            }

            //check if player hit border while facing it
            if ((rotateTransform.Angle == 270 && playerTop == -620) //top
                || (rotateTransform.Angle == 90 && playerTop == 620) //down
                || (rotateTransform.Angle == 180 && playerLeft == -620) //left
                || (rotateTransform.Angle == 0 && playerLeft == 620)) //right
            {
                Crash();
            }

            //check if player immediately goes in the opposite direction and crashes into baby duck
            while (collectedDucks.Count > 0)
            {
                if ((lastDirection == Windows.System.VirtualKey.Up && currentDirection == Windows.System.VirtualKey.Down)
                    || (lastDirection == Windows.System.VirtualKey.Down && currentDirection == Windows.System.VirtualKey.Up)
                    || (lastDirection == Windows.System.VirtualKey.Left && currentDirection == Windows.System.VirtualKey.Right)
                    || (lastDirection == Windows.System.VirtualKey.Right && currentDirection == Windows.System.VirtualKey.Left))
                {
                    await Task.Delay(40);
                    Crash();
                    return;
                }
                break;
            }
            lastDirection = currentDirection;
        }

        //check collision with dot or ducks
        private bool CollisionDetected(GamePiece playerPiece, GamePiece gamePiece)
        {
            var playerBounds = playerPiece.PieceImg.TransformToVisual(gridMain)
                .TransformBounds(new Rect(0, 0, playerPiece.PieceImg.ActualWidth, playerPiece.PieceImg.ActualHeight));

            var pieceBounds = gamePiece.PieceImg.TransformToVisual(gridMain)
                .TransformBounds(new Rect(0, 0, (gamePiece.PieceImg.ActualWidth), (gamePiece.PieceImg.ActualHeight / 2)));

            return (playerBounds.Left < pieceBounds.Right &&
                    playerBounds.Right > pieceBounds.Left &&
                    playerBounds.Top < pieceBounds.Bottom &&
                    playerBounds.Bottom > pieceBounds.Top);
        }

        #endregion

        #region Level Settings

        //reset game and update level (speed) depending on score reached
        private void CheckLevelUp()
        {
            switch (score)
            {
                //when score hits 50, go to level 2
                case 50:
                    message = "Level 2\n\n";
                    setScore = 50;
                    setSpeed = 0;
                    ResetGame();
                    CheckSpeed();
                    //overlay
                    screenOverlay.Visibility = Visibility.Visible;
                    lblOverlay.Visibility = Visibility.Visible;
                    btnScores.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }

        //update text for speed level
        private void CheckSpeed()
        {
            switch (speed) 
            {
                case 10: //default
                    txtSpeed.Text = "Normal".ToUpper();
                    break;
                case 0: //fast
                    txtSpeed.Text = "Fast".ToUpper();
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Initialize

        private async void InitializeMediaPlayer()
        {
            StorageFolder appInstalled = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder assetsFolder = await appInstalled.GetFolderAsync("Assets");

            //get the file from the assets folder
            StorageFile chirpFile = await assetsFolder.GetFileAsync("chirp.wav");
            StorageFile quackFile = await assetsFolder.GetFileAsync("quack.wav");

            chirp.AutoPlay = false;
            chirp.Source = MediaSource.CreateFromStorageFile(chirpFile);

            quack.AutoPlay = false;
            quack.Source = MediaSource.CreateFromStorageFile(quackFile);
        }

        private void AlignBabyDucks()
        {
            //add new position and rotation angle of player
            playerStates.Add(new PlayerState { Position = player.Location, RotationAngle = player.RotateTransform.Angle });

            int offsetPerBaby = 5; //spacing between baby ducks
            int distanceBehind = 3; //starting distance behind player

            for (int i = 0; i < collectedDucks.Count; i++)
            {
                int index = playerStates.Count - ((i + 1) * offsetPerBaby) - distanceBehind;

                if (index >= 0)
                {
                    // Set the position and rotation of the playerBaby based on the recorded states
                    collectedDucks[i].Location = playerStates[index].Position;
                    collectedDucks[i].RotateTransform.Angle = playerStates[index].RotationAngle;
                }
            }
        }

        private void MovePieceRandomly(GamePiece piece, int pieceSpeed)
        {
            piece.PlayerSpeed = pieceSpeed;

            if (piece != null)
            {
                //if target position and current position properties are not set
                if (piece.TargetPosition == null || piece.CurrentPosition == null)
                {
                    piece.CurrentPosition = new Point(0, 0); //set current position to top left
                    SetNewTarget(piece); //create new target position
                }

                //move piece towards target position
                double deltaX = piece.TargetPosition.X - piece.CurrentPosition.X;
                double deltaY = piece.TargetPosition.Y - piece.CurrentPosition.Y;

                //calculate distance
                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                //if distance is greater than speed
                if (distance > piece.PlayerSpeed)
                {
                    //calculate additional distance to move based on speed
                    double moveX = (deltaX / distance) * piece.PlayerSpeed;
                    double moveY = (deltaY / distance) * piece.PlayerSpeed;

                    //update current position value
                    piece.CurrentPosition = new Point(piece.CurrentPosition.X + moveX, piece.CurrentPosition.Y + moveY);

                    //update location
                    piece.Location = new Thickness(piece.CurrentPosition.X, piece.CurrentPosition.Y, 0, 0);
                }
                else
                {
                    //after target position has been reached, set new target
                    SetNewTarget(piece);
                }
            }
        }

        //generate new target position
        private void SetNewTarget(GamePiece piece)
        {
            int gridSize = 620;
            int randomX = random.Next(0, gridSize - 30);
            int randomY = random.Next(0, gridSize - 30);

            piece.TargetPosition = new Point(randomX, randomY);
        }

        private GamePiece CreatePiece(string imgSrc, int size, int left, int top, bool alignTopLeft = false)
        {
            Image img = new Image();
            img.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{imgSrc}.png"));
            img.Width = size;
            img.Height = size;
            img.Name = $"img{imgSrc}";
            img.Margin = new Thickness(left, top, 0, 0);

            //if piece needs to be aligned from top and left rather than center
            if (alignTopLeft)
            {
                img.VerticalAlignment = VerticalAlignment.Top;
                img.HorizontalAlignment = HorizontalAlignment.Left;
            }
            else
            {
                img.VerticalAlignment = VerticalAlignment.Center;
                img.HorizontalAlignment = HorizontalAlignment.Center;
            }

            gridMain.Children.Add(img);

            return new GamePiece(img);
        }

        #endregion

        #region Buttons
        private void btnScores_Click(object sender, RoutedEventArgs e)
        {
            if (gridScores.Visibility == Visibility.Collapsed)
                gridScores.Visibility = Visibility.Visible;
            else
                gridScores.Visibility = Visibility.Collapsed;
        }

        private async void Quit()
        {
            ResetGame();

            //wait for scores to be overwritten before quitting
            await OverwriteScoresAsync();

            //exit application
            Application.Current.Exit();
        }

        private void Restart()
        {
            ResetGame();

            gridRestart.Visibility = Visibility.Collapsed;

            //overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;
            btnScores.Visibility = Visibility.Visible;
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            Quit();
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            Restart();
        }

        #endregion

    }
}