using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace CvProgram
{
    public sealed class EnterButtonNextControlBehavior : Behavior<UIElement>
    {
        protected override void OnAttached() => AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;

        protected override void OnDetaching() => AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;

        private void AssociatedObject_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (Keyboard.FocusedElement is Button)
            {
                return;
            }

            if (Keyboard.FocusedElement is TextBox textBox && textBox.AcceptsReturn)
            {
                return;
            }

            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
            if (Keyboard.FocusedElement is UIElement elementWithFocus && elementWithFocus.MoveFocus(request))
            {
                e.Handled = true;
            }
        }
    }
}