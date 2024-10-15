using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using System.Text.RegularExpressions;
using Windows.Media.Playback;

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

        //additional properties
        private RotateTransform rotateTransform;
        private Point currentPosition; 
        private Point targetPosition;
        private double playerSpeed; 

        //public properties
        public Thickness Location
        {
            get { return pieceImg.Margin; }
            set { pieceImg.Margin = value; }
        }

        public Point CurrentPosition
        {
            get { return currentPosition; }
            set { currentPosition = value; }
        }

        public Point TargetPosition
        {
            get { return targetPosition; }
            set { targetPosition = value; }
        }

        public double PlayerSpeed
        {
            get { return playerSpeed; }
            set { playerSpeed = value; }
        }
        public Image PieceImg => pieceImg; 
        public RotateTransform RotateTransform => rotateTransform;

        //constructor
        public GamePiece(Image img)  // Creates a piece and a reference to its associated image
        {
            pieceImg = img;
            position = img.Margin;

            rotateTransform = new RotateTransform();
            pieceImg.RenderTransform = rotateTransform;

            //rotate around center of image
            pieceImg.RenderTransformOrigin = new Point(0.5, 0.5);  
        }

        // Calculate a new location for the piece, based on a key press
        public bool Move(Windows.System.VirtualKey direction, int offset)
        {
            //store current position of player in variable
            Thickness newMargins = position;
            int gridSize = 620;

            switch (direction)
            {
                case Windows.System.VirtualKey.Up:
                    //up cannot exceed past -620
                    newMargins.Top = Math.Max(-gridSize, position.Top - offset);
                    rotateTransform.Angle = 270;
                    break;
                case Windows.System.VirtualKey.Down:
                    //top cannot exceed past 620
                    newMargins.Top = Math.Min(gridSize, position.Top + offset);
                    rotateTransform.Angle = 90;
                    break;
                case Windows.System.VirtualKey.Left:
                    //left cannot exceed past -620
                    newMargins.Left = Math.Max(-gridSize, position.Left - offset);
                    rotateTransform.Angle = 180;
                    break;
                case Windows.System.VirtualKey.Right:
                    //right cannot exceed past 620
                    newMargins.Left = Math.Min(gridSize, position.Left + offset);
                    rotateTransform.Angle = 0;
                    break;
                default:
                    return false;
            }

            //update position based on new margins
            position = newMargins;

            //apply new position
            pieceImg.Margin = position;
            return true;
        }
    }
}
