﻿using System;
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

        //sounds
        MediaPlayer chirp = new MediaPlayer(); //chirp sound
        MediaPlayer quack = new MediaPlayer(); //quack sound

        //TODO: Add sound for eating fish

        //change settings for level up
        private string message = "";
        private int setScore = 0;
        private int setSpeed = 10;

        SolidColorBrush highlightBrush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(0xFF, 0x6D, 0xC9, 0xEF));
        SolidColorBrush defaultBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            ReadScores(false);

            //set overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;
            btnScores.Visibility = Visibility.Visible;
            //gridScores.Visibility = Visibility.Collapsed;

            //add event handler to timer
            timer.Tick += HandleMovement;

            // Set the BorderBrush
            btnRestart.BorderBrush = highlightBrush;
            btnQuit.BorderBrush = defaultBrush;

            InitializeMediaPlayer();
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (gridRestart.Visibility == Visibility.Visible) //if restart menu is visible
            {
                // Set focus to the Restart button
                // btnRestart.Focus(FocusState.Programmatic);

                // Check if btnRestart has the specific color and if Enter key is pressed
                if (((SolidColorBrush)btnRestart.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Enter)
                {
                    ResetGame();

                    gridRestart.Visibility = Visibility.Collapsed;

                    //overlay
                    screenOverlay.Visibility = Visibility.Visible;
                    lblOverlay.Visibility = Visibility.Visible;
                    btnScores.Visibility = Visibility.Visible;

                }
                // Check if btnQuit has the specific color and if Enter key is pressed
                else if (((SolidColorBrush)btnQuit.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Enter)
                {
                    Application.Current.Exit();
                }

                // Check if btnRestart has the specific color and if Left arrow key is pressed
                if (((SolidColorBrush)btnRestart.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Left)
                {
                    btnQuit.BorderBrush = highlightBrush;
                    btnRestart.BorderBrush = defaultBrush;
                }

                // Check if btnQuit has the specific color and if Right arrow key is pressed
                if (((SolidColorBrush)btnQuit.BorderBrush).Color == highlightBrush.Color && e.VirtualKey == Windows.System.VirtualKey.Right)
                {
                    btnRestart.BorderBrush = highlightBrush;
                    btnQuit.BorderBrush = defaultBrush;
                }
            }

            if (lblOverlay.Visibility == Visibility.Visible) //if overlay is visible
            {
                if (e.VirtualKey == Windows.System.VirtualKey.Space) //space bar pressed
                    StartGame();

                //if (btnScores.Visibility == Visibility.Visible && e.VirtualKey == Windows.System.VirtualKey.Enter)
                //{
                //    if (gridScores.Visibility == Visibility.Collapsed)
                //        gridScores.Visibility = Visibility.Visible;
                //    else
                //        gridScores.Visibility = Visibility.Collapsed;
                //}
            }
            else if (lblOverlay.Visibility == Visibility.Collapsed || gridRestart.Visibility == Visibility.Collapsed)
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

        #region Level Settings

        //reset game and update level (speed) depending on score reached
        private void CheckLevelUp()
        {
            switch (score)
            {
                case 50:
                    message = "Level 2\n\n";
                    setScore = 50;
                    setSpeed = 0;
                    //PrepReset();
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
            switch (speed) // 5 = faster, 0 = extra fast, -5 = super fast, -10 = extremely fast
            {
                case 10:
                    txtSpeed.Text = "Normal".ToUpper();
                    break;
                case 0:
                    txtSpeed.Text = "Fast".ToUpper();
                    break;
                default:
                    break;
            }
        }

        #endregion

        private void Crash()
        {
            quack.Play();
            message = "";
            setScore = 0;
            setSpeed = defaultSpeed;
            ResetGame();

            //TODO: Add condition to check if score is greater than highscore, and if it is, display gridHighscore instead of gridRestart
            gridRestart.Visibility = Visibility.Visible;
        }

        //reset game
        private void ResetGame()
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

            //if (score > highscore)
            //{
            //    scores.Add(score); //add current score to list of scores
            //}
            scores.Add(score);

            score = setScore; //set new score

            speed = setSpeed; //set new speed

            lblOverlay.Text = $"{message}Press Space to start".ToUpper();

            //clear lists
            collectedDucks.Clear();
            playerStates.Clear();

            lblInfo.Text = ""; //clear text
        }

        #region High Score

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

                txtScores.Text += "\n\n";

                //add each line in file to list of scores
                foreach (string line in fileLines)
                {
                    scores.Add(Int32.Parse(line)); //add to list collection of scores
                    txtScores.Text += $"{line}\n";
                }

                //TODO: Display scores in txtScores

                if (overwrite) //game has ended and storage file has been cleared but updated scores in list
                {
                    // Join all scores into a single string separated by newlines
                    string allScores = string.Join("\n", scores);

                    // Write all the scores at once to the file
                    await FileIO.WriteTextAsync(storageFile, allScores);

                    //foreach (int num in scores)
                    //{
                    //    //overwrite file with values in scores list
                    //    await FileIO.WriteTextAsync(storageFile, $"{num}\n");
                    //}
                }

                SetHighscore();
            }
        }

        private void SetHighscore()
        {
            //find highest score from text file
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

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            ReadScores(true);
            Application.Current.Exit();
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();

            gridRestart.Visibility = Visibility.Collapsed;

            //overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;
            btnScores.Visibility = Visibility.Visible;
        }

        #endregion

    }
}