using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using GameLibrary;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.System.Diagnostics.Telemetry;

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
        private static GamePiece babyDuck;

        //private static List<GamePiece> collectedDots = new List<GamePiece>();
        private static List<GamePiece> collectedDucks = new List<GamePiece>();

        private Random random = new Random();

        private DispatcherTimer timer = new DispatcherTimer();
        private Windows.System.VirtualKey currentDirection;

        private int score = 0;

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

            player = CreatePiece("player", 80, -550, -510);  // img, size, left, top
            dot = CreatePiece("dot", 15, 210, -300);

            playerStates.Add(new PlayerState { Position = player.Location, RotationAngle = 0 });

            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += HandleMovement;
            //timer.Start();
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            //currentDirection = e.VirtualKey;
            //lblOverlay.Visibility = Visibility.Collapsed;
            if (lblOverlay.Visibility == Visibility.Visible)
            {
                // Check if the Spacebar is pressed
                if (e.VirtualKey == Windows.System.VirtualKey.Space)
                {
                    lblOverlay.Visibility = Visibility.Collapsed;
                    // Reset the current direction to prevent immediate movement
                    currentDirection = Windows.System.VirtualKey.Right;
                    // Start the timer to resume the game
                    timer.Start();
                }
                // Ignore other keys
            }
            else
            {
                currentDirection = e.VirtualKey;
            }
        }

        private void HandleMovement(object sender, object e)
        {
            // Move the player continuously in the current direction
            player.Move(currentDirection, 20);

            //txtBottom.Text = player.Location.Bottom.ToString();
            //txtLeft.Text = player.Location.Left.ToString();
            //txtRight.Text = player.Location.Right.ToString();
            //txtTop.Text = player.Location.Top.ToString();

            // Record the player's new position and rotation angle
            playerStates.Add(new PlayerState { Position = player.Location, RotationAngle = player.RotateTransform.Angle });

            // Move the body pieces
            int offsetPerBaby = 5; // Adjust this value to control the spacing between body pieces
            int distanceBehind = 3;

            for (int i = 0; i < collectedDucks.Count; i++)
            {
                int stateIndex = playerStates.Count - ((i + 1) * offsetPerBaby) - distanceBehind;

                if (stateIndex >= 0)
                {
                    // Set the position and rotation of the playerBaby based on the recorded states
                    collectedDucks[i].Location = playerStates[stateIndex].Position;
                    collectedDucks[i].RotateTransform.Angle = playerStates[stateIndex].RotationAngle;
                }
            }

            if (CollisionDetected(player, dot))
            {
                HandleDotCollection();
            }

            // Number of baby ducks to ignore (e.g., the ones immediately behind the player)
            int ignoreCount = 4;

            // Start checking from the baby duck at index 'ignoreCount'
            for (int i = ignoreCount; i < collectedDucks.Count; i++)
            {
                GamePiece babyDuck = collectedDucks[i];
                if (CollisionDetected(player, babyDuck))
                {
                    string msg = $"Game Over!\nScore: {score}";
                    ResetGame(msg, 0, 10); //set score to 0 and speed to 10
                    //lblOverlay.Visibility = Visibility.Visible;
                    //lblOverlay.Text = "Crash";
                    break; // Exit the loop after detecting a collision
                }
            }
        }

        private void HandleDotCollection()
        {
            // Update score and collected dots
            //collectedDots.Add(dot);
            score++;
            txtScore.Text = score.ToString();

            // Create a new playerBaby and add it to the trail
            babyDuck = CreatePiece("player", 40, 700, 700);
            collectedDucks.Add(babyDuck);

            CheckLevelUp();

            // Remove the dot from the screen and spawn a new one
            gridMain.Children.Remove(dot.PieceImg);
            int newLeft = random.Next(0, (int)(gridMain.ActualWidth - 30));
            int newTop = random.Next(0, (int)(gridMain.ActualHeight - 30));
            dot = CreatePiece("dot", 15, newLeft, newTop);
        }

        private void CheckLevelUp()
        {
            switch (score)
            {
                case 25:
                    ResetGame("Level up!", 25, 0); 
                    break;
                case 50:
                    ResetGame("Level up!", 50, -10);
                    break;
                default:
                    // No action needed for other scores
                    break;
            }
        }

        private bool CollisionDetected(GamePiece playerPiece, GamePiece gamePiece)
        {
            var playerBounds = playerPiece.PieceImg.TransformToVisual(gridMain)
                .TransformBounds(new Rect(0, 0, playerPiece.PieceImg.ActualWidth, playerPiece.PieceImg.ActualHeight));

            var pieceBounds = gamePiece.PieceImg.TransformToVisual(gridMain)
                .TransformBounds(new Rect(0, 0, (gamePiece.PieceImg.ActualWidth / 2), (gamePiece.PieceImg.ActualHeight)));

            //lblLeft.Text = playerBounds.ToString();
            //lblRight.Text = pieceBounds.ToString();
            //txtLeft.Text = "";
            //txtRight.Text = "";

            return (playerBounds.Left < pieceBounds.Right &&
                    playerBounds.Right > pieceBounds.Left &&
                    playerBounds.Top < pieceBounds.Bottom &&
                    playerBounds.Bottom > pieceBounds.Top);
        }

        private void ResetGame(string message, int setScore, int speed)
        {

            // Stop the game timer
            timer.Stop();

            // Show overlay or game over screen
            lblOverlay.Visibility = Visibility.Visible;
            lblOverlay.Text = $"{message}\n\nPress Space to start";

            // Remove all game pieces from the screen
            gridMain.Children.Remove(player.PieceImg);
            gridMain.Children.Remove(dot.PieceImg);

            foreach (var babyDuck in collectedDucks)
            {
                gridMain.Children.Remove(babyDuck.PieceImg);
            }

            // Clear the collected trail and player states
            collectedDucks.Clear();
            playerStates.Clear();

            // Reset score
            score = setScore;
            txtScore.Text = setScore.ToString();

            // Reset the current direction
            currentDirection = Windows.System.VirtualKey.None;

            // Add the initial player state
            playerStates.Add(new PlayerState { Position = player.Location, RotationAngle = 0 });

            // Hide the overlay when a key is pressed
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            // Reset player's position and create new player and dot
            player = CreatePiece("player", 80, -550, -510);  // Reset to initial position
            dot = CreatePiece("dot", 15, 210, -300);

            timer.Interval = TimeSpan.FromMilliseconds(speed);

            // Restart the game timer
            //timer.Start();
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

            // Ensure the image rotates around its center
            img.RenderTransformOrigin = new Point(0.5, 0.5);

            gridMain.Children.Add(img);

            return new GamePiece(img);
        }
    }
}
