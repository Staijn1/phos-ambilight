using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Phos.ScreenSync
{
    public partial class OverlayWindow : Window
    {
        private Point startPoint;
        private bool isDragging;
        public event Action<int, int, int, int> AreaSelected;
        
        public OverlayWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.Topmost = true;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(OverlayCanvas);
            Canvas.SetLeft(SelectionRectangle, startPoint.X);
            Canvas.SetTop(SelectionRectangle, startPoint.Y);
            isDragging = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var pos = e.GetPosition(OverlayCanvas);
                var x = Math.Min(pos.X, startPoint.X);
                var y = Math.Min(pos.Y, startPoint.Y);
                var w = Math.Max(pos.X, startPoint.X) - x;
                var h = Math.Max(pos.Y, startPoint.Y) - y;

                SelectionRectangle.Width = w;
                SelectionRectangle.Height = h;
                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            var x = (int)Canvas.GetLeft(SelectionRectangle);
            var y = (int)Canvas.GetTop(SelectionRectangle);
            var w = (int)SelectionRectangle.Width;
            var h = (int)SelectionRectangle.Height;
            
            AreaSelected?.Invoke(x, y, w, h);
        }
    }
}