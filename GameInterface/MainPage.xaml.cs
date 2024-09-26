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


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

/* UWP Game Template
 * Created By: Melissa VanderLely
 * Modified By:  
 */


namespace GameInterface
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static GamePiece player;
        private static GamePiece dot;

        private static List<GamePiece> collectedDots = new List<GamePiece>();  // List to store collected dots
        private Random random = new Random();

        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            player = CreatePiece("player", 100, 50, 50);                      //create a GamePiece object associated with the pac-man image
            dot = CreatePiece("dot", 30, 150, 150);
       }

        /// <summary>
        /// This method creates the Image object (to display the picture) and sets its properties.
        /// It adds the image to the screen.
        /// Then it calls the GamePiece constructor, passing the Image object as a parameter.
        /// </summary>
        /// <param name="imgSrc">Name of the image file</param>
        /// <param name="size">Size in pixels (used for both dimensions, the images are square)</param>
        /// <param name="left">Left location relative to parent</param>
        /// <param name="top">Top location relative to parent</param>
        /// <returns></returns>
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

        private async void CoreWindow_KeyDown(object sender, Windows.UI.Core.KeyEventArgs e)
        {
            //Calculate new location for the player character
            player.Move(e.VirtualKey);

            //Check for collisions between player and collectible
            //Note: this looks for identical top/left locations of the two objects. To be more precise, you can write a better collision detection method!

            //if (player.Location == dot.Location)
            //    await new MessageDialog("Collision Detected").ShowAsync();

            // Check for collisions between player and dot
            if (IsCollected(player, dot))
            {
                // Add the collected dot to the list
                collectedDots.Add(dot);

                // Remove the dot from the screen
                gridMain.Children.Remove(dot.OnScreen);

                // Notify the player
                //await new MessageDialog("Dot Collected!").ShowAsync();

                // Spawn a new dot at a random location
                int newLeft = random.Next(0, (int)(gridMain.ActualWidth - 30)); // Prevents spawning off-screen
                int newTop = random.Next(0, (int)(gridMain.ActualHeight - 30));
                dot = CreatePiece("dot", 30, newLeft, newTop);
            }
        }

        // Improved collision detection method
        private bool IsCollected(GamePiece playerPiece, GamePiece dotPiece)
        {
            // Get the player's and dot's bounds
            var playerBounds = playerPiece.OnScreen.TransformToVisual(gridMain)
                .TransformBounds(new Windows.Foundation.Rect(0, 0, playerPiece.OnScreen.ActualWidth, playerPiece.OnScreen.ActualHeight));

            var dotBounds = dotPiece.OnScreen.TransformToVisual(gridMain)
                .TransformBounds(new Windows.Foundation.Rect(0, 0, dotPiece.OnScreen.ActualWidth, dotPiece.OnScreen.ActualHeight));

            // Manually check for intersection between the two rectangles
            return (playerBounds.Left < dotBounds.Right &&
                    playerBounds.Right > dotBounds.Left &&
                    playerBounds.Top < dotBounds.Bottom &&
                    playerBounds.Bottom > dotBounds.Top);
        }

    }
}
