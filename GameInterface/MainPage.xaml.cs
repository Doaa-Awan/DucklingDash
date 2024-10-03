using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using GameLibrary;
using Windows.UI.Xaml.Media.Imaging;


/* UWP Game Template
 * Created By: Melissa VanderLely
 * Modified By:  
 */

namespace GameInterface
{
    public sealed partial class MainPage : Page
    {
        private static GamePiece player;
        private static GamePiece dot;

        private static List<GamePiece> collectedDots = new List<GamePiece>();  //List to store collected dots
        private Random random = new Random();

        // Declare the DispatcherTimer and direction
        private DispatcherTimer timer = new DispatcherTimer();
        private Windows.System.VirtualKey currentDirection;

        private int playerSpeed = 50;

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            player = CreatePiece("player", 80, 50, 50);  //img, size, left, top
            dot = CreatePiece("dot", 15, 150, 150);

            timer.Interval = TimeSpan.FromMilliseconds(playerSpeed); 
            timer.Tick += HandleMovement; 
            timer.Start();
        }

        private void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            currentDirection = e.VirtualKey;
            lblOverlay.Visibility = Visibility.Collapsed;
        }

        private void HandleMovement(object sender, object e)
        {
            // Move the player continuously in the current direction
            player.Move(currentDirection, gridMain.Height, gridMain.Width);

            // After moving, check for collisions
            if (IsCollected(player, dot))
            {
                // Handle the dot collection
                HandleDotCollection();
            }
        }

        private void HandleDotCollection()
        {
            // Add the collected dot to the list
            collectedDots.Add(dot);

            //update score
            txtScore.Text = (collectedDots.Count).ToString();

            // Remove the dot from the screen
            gridMain.Children.Remove(dot.PieceImg);

            // Spawn a new dot at a random location
            int newLeft = random.Next(0, (int)(gridMain.ActualWidth - 30)); // Prevents spawning off-screen
            int newTop = random.Next(0, (int)(gridMain.ActualHeight - 30));
            dot = CreatePiece("dot", 15, newLeft, newTop);
        }

        private bool IsCollected(GamePiece playerPiece, GamePiece dotPiece)
        {
            // Get the player's and dot's bounds
            var playerBounds = playerPiece.PieceImg.TransformToVisual(gridMain)
                .TransformBounds(new Windows.Foundation.Rect(0, 0, playerPiece.PieceImg.ActualWidth, playerPiece.PieceImg.ActualHeight));

            var dotBounds = dotPiece.PieceImg.TransformToVisual(gridMain)
                .TransformBounds(new Windows.Foundation.Rect(0, 0, dotPiece.PieceImg.ActualWidth, dotPiece.PieceImg.ActualHeight));

            // Check if the player and the dot are intersecting
            return (playerBounds.Left < dotBounds.Right &&
                    playerBounds.Right > dotBounds.Left &&
                    playerBounds.Top < dotBounds.Bottom &&
                    playerBounds.Bottom > dotBounds.Top);
        }

        private GamePiece CreatePiece(string imgSrc, int size, int left, int top)
        {
            Image img = new Image();
            img.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{imgSrc}.png"));
            img.Width = size;
            img.Height = size;
            img.Name = $"img{imgSrc}";
            img.Margin = new Thickness(left, top, 0, 0);
            img.VerticalAlignment = VerticalAlignment.Top;
            img.HorizontalAlignment = HorizontalAlignment.Left;

            gridMain.Children.Add(img);

            return new GamePiece(img);
        }


    }
}
