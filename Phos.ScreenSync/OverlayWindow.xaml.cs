using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Phos.ScreenSync
{
    public partial class OverlayWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging;
        public event Action<int, int, int, int> AreaSelected;
        
        public OverlayWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            Topmost = true;
            KeyDown += OverlayWindow_KeyDown;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(OverlayCanvas);
            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            _isDragging = true;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var pos = e.GetPosition(OverlayCanvas);
                var x = Math.Min(pos.X, _startPoint.X);
                var y = Math.Min(pos.Y, _startPoint.Y);
                var w = Math.Max(pos.X, _startPoint.X) - x;
                var h = Math.Max(pos.Y, _startPoint.Y) - y;

                SelectionRectangle.Width = w;
                SelectionRectangle.Height = h;
                Canvas.SetLeft(SelectionRectangle, x);
                Canvas.SetTop(SelectionRectangle, y);
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            var x = (int)Canvas.GetLeft(SelectionRectangle);
            var y = (int)Canvas.GetTop(SelectionRectangle);
            var w = (int)SelectionRectangle.Width;
            var h = (int)SelectionRectangle.Height;
            
            AreaSelected?.Invoke(x, y, w, h);
        }
        
        private void OverlayWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}