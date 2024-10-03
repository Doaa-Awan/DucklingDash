using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

/* UWP Game Library
 * Written By: 
 * Modified By:
 */

namespace GameLibrary
{
    public class GamePiece
    {
        private Thickness objectMargins;            //represents the location of the piece on the game board
        private Image onScreen;                     //the image that is displayed on screen
        private RotateTransform rotateTransform; // Transform to rotate the image

        public Thickness Location                     //get access only - can not directly modify the location of the piece
        {
            get { return onScreen.Margin; }
        }

        public Image OnScreen => onScreen; //public property to access image

        public GamePiece(Image img)                 //constructor creates a piece and a reference to its associated image
        {                                           //use this to set up other GamePiece properties
            onScreen = img;
            objectMargins = img.Margin;

            // Initialize RotateTransform and set it as the image's render transform
            rotateTransform = new RotateTransform();
            onScreen.RenderTransform = rotateTransform;
            onScreen.RenderTransformOrigin = new Point(0.5, 0.5);  // Rotate around the center of the image
        }

        public bool Move(Windows.System.VirtualKey direction)   //calculate a new location for the piece, based on a key press
        {
            // Get the current margin (location) of the player
            Thickness newMargins = objectMargins;

            switch (direction)
            {
                case Windows.System.VirtualKey.Up:
                    newMargins.Top = Math.Max(0, objectMargins.Top - 20);  // Ensure the top margin doesn't go below 0
                    rotateTransform.Angle = 270;  // Rotate upwards (270 degrees)
                    //objectMargins.Top -= 10;
                    break;
                case Windows.System.VirtualKey.Down:
                    newMargins.Top = Math.Min(700 - onScreen.Height, objectMargins.Top + 20);  // Prevent the player from moving below the grid
                    rotateTransform.Angle = 90;  // Rotate downwards (90 degrees)
                    //objectMargins.Top += 10;
                    break;
                case Windows.System.VirtualKey.Left:
                    newMargins.Left = Math.Max(0, objectMargins.Left - 20);  // Ensure the left margin doesn't go below 0
                    rotateTransform.Angle = 180;  // Rotate left (180 degrees)
                    //objectMargins.Left -= 10;
                    break;
                case Windows.System.VirtualKey.Right:
                    newMargins.Left = Math.Min(700 - onScreen.Width, objectMargins.Left + 20);  // Prevent the player from moving beyond the grid
                    rotateTransform.Angle = 0;  // Rotate right (0 degrees)
                    //objectMargins.Left += 10;
                    break;
                default:
                    return false;
            }
            // Update only if the new location is within boundaries
            objectMargins = newMargins;

            onScreen.Margin = objectMargins;            //assign the new position to the on-screen image
            return true;
        }


    }
}
