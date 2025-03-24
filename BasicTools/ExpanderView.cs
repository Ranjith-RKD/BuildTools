using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
namespace BasicTool
{
    public class ExpanderView : Grid
    {
        private RowDefinition _contentRow;
        private ContentView _overContent;
        private Grid _overHeaderGrid;
        private bool _isExpanded = true;

        // Title (Header) View
        public static readonly BindableProperty HeaderProperty =
            BindableProperty.Create(nameof(Header), typeof(IView), typeof(ExpanderView), null, propertyChanged: OnHeaderChanged);

        public IView Header
        {
            get => (View)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        // Content View
        public static readonly BindableProperty ContentProperty =
            BindableProperty.Create(nameof(Content), typeof(IView), typeof(ExpanderView), null, propertyChanged: OnContentChanged);

        public IView Content
        {
            get => (View)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        // Expand/Collapse Icon
        private Label _iconLabel;

        public ExpanderView()
        {
            _overContent = new ContentView();
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Initialize content row expanded by default
            _contentRow = new RowDefinition { Height = GridLength.Auto };
            RowDefinitions.Add(_contentRow);

            // Initialize icon label
            _iconLabel = new Label
            {
                FontFamily = "appicons",
                Text = "\uf106", // Arrow icon
                FontSize = 20,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center,
                Rotation = 0, // Expanded by default
                TextColor = Colors.Gray
            };

            // Add TapGesture to toggle expand/collapse
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => ToggleContent();


            BuildLayout();
            _overHeaderGrid.GestureRecognizers.Add(tapGesture);
        }
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
        }
        private void BuildLayout()
        {
            // Header with icon
            _overHeaderGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = 50 }
                }
            };

            _overHeaderGrid.Add(_iconLabel, 1);
            this.Add(_overHeaderGrid, 0, 0); // Initially expanded
            this.Add(_overContent,0, 1);
        }

        private static void OnHeaderChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if(newValue == null) return;
            var expander = (ExpanderView)bindable;
            expander._overHeaderGrid.Add(newValue as View, 0, 0);
        }

        private static void OnContentChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue == null) return;
            var expander = (ExpanderView)bindable;
            expander._overContent.Content = newValue as View;
        }

        private async void ToggleContent()
        {
            if (Content == null) return;

            if (_isExpanded)
            {
                await CollapseContent();
            }
            else
            {
                await ExpandContent();
            }

            _isExpanded = !_isExpanded;

            // Rotate the arrow icon during expansion and collapse
            await _iconLabel.RotateTo(_isExpanded ? 0 : 180, 250, Easing.CubicInOut);
        }

        private async Task ExpandContent()
        {
            _overContent.IsVisible = true;
            (_overContent as IView).Measure((this.Parent.Parent as VisualElement).Bounds.Width, double.PositiveInfinity);
            double targetHeight = _overContent.DesiredSize.Height;

            await this.AnimateAsync("expand",
                x => _contentRow.Height = new GridLength(x),
                0, targetHeight, 16, 1000, Easing.CubicInOut);
        }

        private async Task CollapseContent()
        {
            double initialHeight = _overContent.Bounds.Height;

            await this.AnimateAsync("collapse",
                x => _contentRow.Height = new GridLength(x),
                initialHeight, 0, 16, 1000, Easing.CubicInOut, _overContent);
        }
    }

    public static class AnimationExtensions
    {
        public static Task<bool> AnimateAsync(this VisualElement element, string name, Action<double> callback,
            double start, double end, uint rate, uint length, Easing easing)
        {
            var tcs = new TaskCompletionSource<bool>();
            Animation animation = new Animation(callback, start, end);
            element.Animate(name, animation, rate, length, easing, finished: (v, c) => tcs.SetResult(c));
            return tcs.Task;
        }

        public static Task<bool> AnimateAsync(this VisualElement element, string name, Action<double> callback,
           double start, double end, uint rate, uint length, Easing easing, View _overContent)
        {
            var tcs = new TaskCompletionSource<bool>();
            Animation animation = new Animation(callback, start, end);
            element.Animate(name, animation, rate, length, easing, finished: (v, c) => { _overContent.IsVisible = false; tcs.SetResult(c); });
            return tcs.Task;
        }
    }
}
