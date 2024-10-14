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

        //initalize game pieces
        private static GamePiece player;
        private static GamePiece dot;
        private static GamePiece bonusDot;
        private static GamePiece babyDuck;
        private static GamePiece enemy;

        //list of baby ducks
        private static List<GamePiece> collectedDucks = new List<GamePiece>();

        //list of player positions and angles
        private List<PlayerState> playerStates = new List<PlayerState>();

        //list to hold scores
        List<int> scores = new List<int>();

        private Random random = new Random(); //random number generator
        private DispatcherTimer timer = new DispatcherTimer(); //timer
        private Windows.System.VirtualKey currentDirection; //current key press direction
        private Windows.System.VirtualKey lastDirection;

        //set variables 
        private int score = 0;
        private int highscore = 0;
        private int speed = 10;
        private int defaultSpeed = 10;

        //StorageFolder assetsFolder;
        MediaPlayer chirp = new MediaPlayer(); //chirp sound
        //bool soundPlaying = false;

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            //set overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;

            //add event handler to timer
            timer.Tick += HandleMovement;

            InitializeMediaPlayer();
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (lblOverlay.Visibility == Visibility.Visible) //if overlay is visible
            {
                if (e.VirtualKey == Windows.System.VirtualKey.Space) //space bar pressed
                {
                    ReadScores(false);

                    //disable overlay
                    lblOverlay.Visibility = Visibility.Collapsed;
                    screenOverlay.Visibility = Visibility.Collapsed;

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
            }
            else
            {
                //if overlay is not visible change direction based on key press
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
                    default:
                        currentDirection = e.VirtualKey;
                        break;
                }

                //currentDirection = e.VirtualKey;
            }
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
                ResetGame("Game Over", 0, defaultSpeed);
            }

            //if fish does not exist and score hits certain points, generate fish
            if (bonusDot == null && (score == 10 || score == 35 || score == 50))
            {
                bonusDot = CreatePiece("fish", 40, 0, 0, true);
                lblInfo.Text = "ducks can eat fish";
            }

            //if enemy does not exist and score hits 15
            if (enemy == null && (score == 20 || score == 60))
            {
                enemy = CreatePiece("turtle", 80, 0, 0, true);
                lblInfo.Text = "do not eat the turtle";
            }

            AlignBabyDucks(); //ensure baby ducks follow directly behind player
            CheckCrash(); //check if player hits border or baby ducks
        }

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
           
            if(score > highscore)
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

        //reset game and update level (speed) depending on score reached
        private void CheckLevelUp()
        {
            switch (score)
            {
                case 40:
                    ResetGame("Level 2", 40, 0); //faster = 5
                    CheckSpeed();
                    break;
                //case 65:
                //    ResetGame("Level 3", 65, -10); //extra fast = 0
                //    CheckSpeed();
                //    break;
                //case 85:
                //    ResetGame("Level 4", 85, -5); //super fast = -5
                //    CheckSpeed();
                //    break;
                //case 100:
                //    ResetGame("Level 5", 100, -10); //extreme = -10
                //    CheckSpeed();
                //    break;
                default:
                    break;
            }
        }

        //update text for speed level
        private void CheckSpeed()
        {
            switch (speed) // 5 = faster, 0 = extra fast, -5 = super fast, -10 = extremely fast
            {
                case 10:
                    txtSpeed.Text = "Normal".ToUpper();
                    break;
                case 0:
                    txtSpeed.Text = "Faster".ToUpper();
                    break;
                default:
                    break;
            }
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
                    ResetGame("Game Over", 0, defaultSpeed); //set score to 0 and reset speed
                    break;
                }
            }

            //check if player hit border while facing it
            if ((rotateTransform.Angle == 270 && playerTop == -620) //top
                || (rotateTransform.Angle == 90 && playerTop == 620) //down
                || (rotateTransform.Angle == 180 && playerLeft == -620) //left
                || (rotateTransform.Angle == 0 && playerLeft == 620)) //right
                ResetGame("Game Over", 0, defaultSpeed);

            //check if player immediately goes in the opposite direction and crashes into baby duck
            while(collectedDucks.Count > 0)
            {
                if ((lastDirection == Windows.System.VirtualKey.Up && currentDirection == Windows.System.VirtualKey.Down)
                    || (lastDirection == Windows.System.VirtualKey.Down && currentDirection == Windows.System.VirtualKey.Up)
                    || (lastDirection == Windows.System.VirtualKey.Left && currentDirection == Windows.System.VirtualKey.Right)
                    || (lastDirection == Windows.System.VirtualKey.Right && currentDirection == Windows.System.VirtualKey.Left))
                    {
                        await Task.Delay(50);
                        ResetGame("Game Over", 0, defaultSpeed);
                        return;
                    }
                break;
            }
            lastDirection = currentDirection;                
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

        private async void ReadScores(bool overwrite)
        {
            StorageFolder appInstalled = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder assetsFolder = await appInstalled.GetFolderAsync("Assets");

            //get the file from the assets folder
            StorageFile storageFile = await assetsFolder.GetFileAsync("scores.txt");

            if (storageFile != null)
            {
                //read the file contents into an IList collection of strings
                IList<string> fileLines = await FileIO.ReadLinesAsync(storageFile);

                //add each line in file to list of scores
                foreach (string line in fileLines)
                {
                    scores.Add(Int32.Parse(line));
                }

                SetHighscore();
            }
            else if (overwrite) //game has ended and storage file has been cleared but updated scores in list
            {
                foreach (int num in scores)
                {
                    //overwrite file with values in scores list
                    await FileIO.WriteTextAsync(storageFile, $"{num}\n");
                }

                SetHighscore();
            }

            //quit game = clear file/ overwrite scores
            //reset game = add new high score
            //compare scores to get highest and set new highscore as that one

            //using (StreamReader stream = new StreamReader($"Assets/DataSet01.txt"))
            //{
            //    while (await stream.ReadLineAsync() is string line)
            //    {
            //        var pieces = line.Split(",");
            //        Person pr = new Person(pieces[0], pieces[1], pieces[2], pieces[3], int.Parse(pieces[4]));
            //        allPersons.Add(pr);
            //    }
            //}

        }

        private void SetHighscore()
        {
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

        //reset game
        private void ResetGame(string message, int setScore, int setSpeed)
        {
            if (score > highscore)
            {
                scores.Add(score); //add current score to list of scores
            }

            // Display a dialog asking if the user wants to restart the game
            //var messageDialog = new MessageDialog("Do you want to restart the game?", "Game Over");
            //messageDialog.Commands.Add(new UICommand("Yes", null, 0));
            //messageDialog.Commands.Add(new UICommand("No", null, 1));

            //// Set the default command to "Yes" (so Enter acts as confirmation)
            //messageDialog.DefaultCommandIndex = 0;
            //messageDialog.CancelCommandIndex = 1;

            //var result = await messageDialog.ShowAsync();

            //// Check if the user selected "No", in which case exit the game
            //if ((int)result.Id == 1)
            //{
            //    ReadScores(true); //overwrite scores file with list of scores
            //    Application.Current.Exit(); // Exits the app
            //    return;
            //}

            //SetHighscore(); //check for new highscore

            timer.Stop(); //stop timer

            currentDirection = Windows.System.VirtualKey.None; //reset direction

            //reset score and speed

            score = setScore; //set new score

            speed = setSpeed; //set new speed
           
            //overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;
            lblOverlay.Text = $"{message}\n\nPress Space to start".ToUpper();

            //remove all game pieces from the screen
            gridMain.Children.Remove(player.PieceImg);
            gridMain.Children.Remove(dot.PieceImg);

            if(bonusDot != null) //if bonusDot exists, remove it
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

            //clear lists
            collectedDucks.Clear();
            playerStates.Clear();

            lblInfo.Text = ""; //clear text
        }

        private async void InitializeMediaPlayer()
        {
            StorageFolder appInstalled = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder assetsFolder = await appInstalled.GetFolderAsync("Assets");

            //get the file from the assets folder
            StorageFile file = await assetsFolder.GetFileAsync("chirp.wav");

            chirp.AutoPlay = false;
            chirp.Source = MediaSource.CreateFromStorageFile(file);
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
    }
}
