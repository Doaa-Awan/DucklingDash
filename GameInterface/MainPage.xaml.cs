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

/* UWP Game Template
 * Created By: Melissa VanderLely
 * Modified By: Doaa Awan
 */

namespace GameInterface
{
    public sealed partial class MainPage : Page
    {
        private static GamePiece player;
        private static GamePiece dot;
        private static GamePiece bonusDot;
        private static GamePiece babyDuck;

        private static List<GamePiece> collectedDucks = new List<GamePiece>();

        private Random random = new Random();

        private DispatcherTimer timer = new DispatcherTimer();
        private Windows.System.VirtualKey currentDirection;

        private int score = 0;
        private int speed = 10;
        private int defaultSpeed = 10;

        private struct PlayerState
        {
            public Thickness Position;
            public double RotationAngle;
        }

        private List<PlayerState> playerStates = new List<PlayerState>();

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;

            timer.Tick += HandleMovement;
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (lblOverlay.Visibility == Visibility.Visible)
            {
                if (e.VirtualKey == Windows.System.VirtualKey.Space) //space bar pressed
                {
                    lblOverlay.Visibility = Visibility.Collapsed;
                    screenOverlay.Visibility = Visibility.Collapsed;

                    currentDirection = Windows.System.VirtualKey.Right;

                    txtScore.Text = score.ToString();

                    player = CreatePiece("player", 80, -550, -510);  // img, size, left, top
                    dot = CreatePiece("dot", 15, 210, -300);

                    playerStates.Add(new PlayerState { Position = player.Location, RotationAngle = 0 });

                    timer.Interval = TimeSpan.FromMilliseconds(speed);

                    CheckSpeed();

                    timer.Start();
                }
            }
            else
            {
                currentDirection = e.VirtualKey;
            }
        }

        private void HandleMovement(object sender, object e)
        {
            //move player continuously in current direction
            player.Move(currentDirection, 20);

            if (CollisionDetected(player, dot))
            {
                HandleDotCollection();
            }

            AlignBabyDucks();

            CheckCrash();

            if (score == 5)
            {
                bonusDot = CreatePiece("dot-clr", 15, 590, 590);
                if (CollisionDetected(player, bonusDot))
                {
                    HandleDotCollection();
                }
            }

            //bonusDot = CreatePiece("dot-colour", 15, 590, 590);

            //if (bonusDot != null)
            //{
            //    lblTest.Text = bonusDot.Colour;
            //}
        }

        private void HandleDotCollection()
        {
            //add to the score
            score++;
            txtScore.Text = score.ToString();

            //add baby duck
            babyDuck = CreatePiece("player", 40, 700, 700);
            collectedDucks.Add(babyDuck);

            //remove dot and spawn new one
            SpawnNewDot();

            //check score for level up
            CheckLevelUp();
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

        private void SpawnNewDot()
        {
            int gridSize = 620;
            int randomNum = random.Next(0, gridSize - 30);
            gridMain.Children.Remove(dot.PieceImg);
            dot = CreatePiece("dot", 15, randomNum, randomNum);
        }

        private void CheckCrash()
        {
            //hit duck
            int ignoreCount = 3; //ducks immediately behind player to ignore

            //start checking from the baby duck at index 'ignoreCount'
            for (int i = ignoreCount; i < collectedDucks.Count; i++)
            {
                GamePiece babyDuck = collectedDucks[i];
                if (CollisionDetected(player, babyDuck))
                {
                    ResetGame("Game Over", 0, defaultSpeed); //set score to 0 and reset speed
                    break;
                }
            }

            //hit border
            int playerLeft = (int)player.Location.Left;
            int playerTop = (int)player.Location.Top;
            RotateTransform rotateTransform = player.RotateTransform;

            if ((rotateTransform.Angle == 270 && playerTop == -620) //top
                || (rotateTransform.Angle == 90 && playerTop == 620) //down
                || (rotateTransform.Angle == 180 && playerLeft == -620) //left
                || (rotateTransform.Angle == 0 && playerLeft == 620)) //right
                ResetGame("Game Over", 0, defaultSpeed);
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

        //reset game and update level (speed) depending on score reached
        private void CheckLevelUp()
        {
            switch (score)
            {
                case 30:
                    ResetGame("Level 2", 30, 0);
                    CheckSpeed();
                    break;
                case 50:
                    ResetGame("Level 3", 50, -10);
                    CheckSpeed();
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
                case 10:
                    txtSpeed.Text = "Normal".ToUpper();
                    break;
                case 0:
                    txtSpeed.Text = "Faster".ToUpper();
                    break;
                case -10:
                    txtSpeed.Text = "Fastest".ToUpper();
                    break;
                default:
                    break;
            }
        }

        private void ResetGame(string message, int setScore, int setSpeed)
        {
            timer.Stop(); //stop timer

            currentDirection = Windows.System.VirtualKey.None; //reset direction

            score = setScore; //set new score

            speed = setSpeed; //set new speed

            //overlay
            screenOverlay.Visibility = Visibility.Visible;
            lblOverlay.Visibility = Visibility.Visible;
            lblOverlay.Text = $"{message}\n\nPress Space to start".ToUpper();

            //remove all game pieces from the screen
            gridMain.Children.Remove(player.PieceImg);
            gridMain.Children.Remove(dot.PieceImg);

            foreach (var babyDuck in collectedDucks)
            {
                gridMain.Children.Remove(babyDuck.PieceImg);
            }

            //clear lists
            collectedDucks.Clear();
            playerStates.Clear();
        }

        private GamePiece CreatePiece(string imgSrc, int size, int left, int top)
        {
            Image img = new Image();
            img.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{imgSrc}.png"));
            img.Width = size;
            img.Height = size;
            img.Name = $"img{imgSrc}";
            img.Margin = new Thickness(left, top, 0, 0);
            img.VerticalAlignment = VerticalAlignment.Center;
            img.HorizontalAlignment = HorizontalAlignment.Center;

            gridMain.Children.Add(img);

            return new GamePiece(img);
        }
    }
}
