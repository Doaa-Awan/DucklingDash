using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Text.RegularExpressions;

/* UWP Game Library
 * Written By: Melissa VanderLely
 * Modified By: Doaa Awan
 */

namespace GameLibrary
{
    public class GamePiece
    {
        private Thickness position;
        private Image pieceImg;
        private RotateTransform rotateTransform;

        public Thickness Location
        {
            get { return pieceImg.Margin; }
            set { pieceImg.Margin = value; }
        }

        public Image PieceImg => pieceImg; // Public property to access image

        // Public property to access rotation
        public RotateTransform RotateTransform => rotateTransform;

        public GamePiece(Image img)  // Creates a piece and a reference to its associated image
        {
            pieceImg = img;
            position = img.Margin;

            // Initialize RotateTransform and set it as the image's render transform
            rotateTransform = new RotateTransform();
            pieceImg.RenderTransform = rotateTransform;
            pieceImg.RenderTransformOrigin = new Point(0.5, 0.5);  // Rotate around the center of the image
        }

        // Calculate a new location for the piece, based on a key press
        public bool Move(Windows.System.VirtualKey direction, int offset)
        {
            // Get the current margin (location) of the player
            Thickness newMargins = position;

            switch (direction)
            {
                case Windows.System.VirtualKey.Up:
                    // Decrease Top to move up, but not beyond -620
                    newMargins.Top = Math.Max(-620, position.Top - offset);
                    rotateTransform.Angle = 270;
                    break;
                case Windows.System.VirtualKey.Down:
                    // Increase Top to move down, but not beyond 620
                    newMargins.Top = Math.Min(620, position.Top + offset);
                    rotateTransform.Angle = 90;
                    break;
                case Windows.System.VirtualKey.Left:
                    // Decrease Left to move left, but not beyond -620
                    newMargins.Left = Math.Max(-620, position.Left - offset);
                    rotateTransform.Angle = 180;
                    break;
                case Windows.System.VirtualKey.Right:
                    // Increase Left to move right, but not beyond 620
                    newMargins.Left = Math.Min(620, position.Left + offset);
                    rotateTransform.Angle = 0;
                    break;
                default:
                    return false;
            }

            // Update the position with the new margins
            position = newMargins;

            // Apply the new position to the on-screen image
            pieceImg.Margin = position;
            return true;
        }
    }
}
