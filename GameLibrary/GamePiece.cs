using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

/* UWP Game Library
 * Written By: Melissa VanderLely
 * Modified By:
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
        }

        public Image PieceImg => pieceImg; //public property to access image

        public GamePiece(Image img)  //creates a piece and a reference to its associated image
        {
            pieceImg = img;
            position = img.Margin;

            // Initialize RotateTransform and set it as the image's render transform
            rotateTransform = new RotateTransform();
            pieceImg.RenderTransform = rotateTransform;
            pieceImg.RenderTransformOrigin = new Point(0.5, 0.5);  // Rotate around the center of the image
        }

        //calculate a new location for the piece, based on a key press
        public bool Move(Windows.System.VirtualKey direction, double gridHeight, double gridWidth)   
        {

            // Get the current margin (location) of the player
            Thickness newMargins = position;

            switch (direction)
            {
                case Windows.System.VirtualKey.Up:
                    newMargins.Top = Math.Max(0, position.Top - 10);
                    rotateTransform.Angle = 270;
                    break;
                case Windows.System.VirtualKey.Down:
                    newMargins.Top = Math.Min(gridHeight - pieceImg.Height, position.Top + 10);
                    rotateTransform.Angle = 90;
                    break;
                case Windows.System.VirtualKey.Left:
                    newMargins.Left = Math.Max(0, position.Left - 10);
                    rotateTransform.Angle = 180;
                    break;
                case Windows.System.VirtualKey.Right:
                    newMargins.Left = Math.Min(gridWidth - pieceImg.Width, position.Left + 10);
                    rotateTransform.Angle = 0;
                    break;
                default:
                    return false;
            }

            // Recheck bounds to ensure the image is fully visible
            newMargins.Left = Math.Max(0, Math.Min(gridWidth - pieceImg.ActualWidth, newMargins.Left));
            newMargins.Top = Math.Max(0, Math.Min(gridHeight - pieceImg.ActualHeight, newMargins.Top));

            // Update only if the new location is within boundaries
            position = newMargins;

            pieceImg.Margin = position;   // Assign the new position to the on-screen image
            return true;

        }
    }
}
