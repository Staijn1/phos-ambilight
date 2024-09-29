using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using Avalonia;
using Avalonia.Controls.Shapes;

namespace Phos.ScreenSync.New.Views
{
    public partial class SelectAreaWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging;

        public event Action<int, int, int, int> AreaSelected;

        public SelectAreaWindow()
        {
            InitializeComponent();
            this.KeyDown += SelectAreaWindow_KeyDown;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            _startPoint = e.GetPosition(this.FindControl<Canvas>("OverlayCanvas"));
            Canvas.SetLeft(this.FindControl<Rectangle>("SelectionRectangle"), _startPoint.X);
            Canvas.SetTop(this.FindControl<Rectangle>("SelectionRectangle"), _startPoint.Y);
            _isDragging = true;
        }

        private void Canvas_PointerMoved(object sender, PointerEventArgs e)
        {
            if (_isDragging)
            {
                var pos = e.GetPosition(this.FindControl<Canvas>("OverlayCanvas"));
                var x = Math.Min(pos.X, _startPoint.X);
                var y = Math.Min(pos.Y, _startPoint.Y);
                var w = Math.Max(pos.X, _startPoint.X) - x;
                var h = Math.Max(pos.Y, _startPoint.Y) - y;

                var selectionRectangle = this.FindControl<Rectangle>("SelectionRectangle");
                selectionRectangle.Width = w;
                selectionRectangle.Height = h;
                Canvas.SetLeft(selectionRectangle, x);
                Canvas.SetTop(selectionRectangle, y);
            }
        }

        private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            var selectionRectangle = this.FindControl<Rectangle>("SelectionRectangle");
            var x = (int)Canvas.GetLeft(selectionRectangle);
            var y = (int)Canvas.GetTop(selectionRectangle);
            var w = (int)selectionRectangle.Width;
            var h = (int)selectionRectangle.Height;

            AreaSelected?.Invoke(x, y, w, h);
        }

        private void SelectAreaWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}