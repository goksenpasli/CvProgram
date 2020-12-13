using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CvProgram
{
    public class MaskedTextBlock : TextBlock
    {
        public static readonly DependencyProperty InputMaskProperty = DependencyProperty.Register("InputMask", typeof(string), typeof(MaskedTextBlock), null);

        public static readonly DependencyProperty PromptCharProperty = DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBlock), new PropertyMetadata('_'));

        public static readonly DependencyProperty UnmaskedTextProperty = DependencyProperty.Register("UnmaskedText", typeof(string), typeof(MaskedTextBlock), new UIPropertyMetadata(string.Empty, Changed));

        private MaskedTextProvider _provider;

        public MaskedTextBlock() => Loaded += MaskedTextBlock_Loaded;

        public string InputMask { get => (string)GetValue(InputMaskProperty); set => SetValue(InputMaskProperty, value); }

        public char PromptChar { get => (char)GetValue(PromptCharProperty); set => SetValue(PromptCharProperty, value); }

        public string UnmaskedText { get => (string)GetValue(UnmaskedTextProperty); set => SetValue(UnmaskedTextProperty, value); }

        private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBlock maskedTextBlock = d as MaskedTextBlock;
            maskedTextBlock._provider = new MaskedTextProvider(maskedTextBlock.InputMask, CultureInfo.CurrentCulture);
            maskedTextBlock._provider.Set(string.IsNullOrWhiteSpace(maskedTextBlock.UnmaskedText) ? string.Empty : e.NewValue as string);
            maskedTextBlock.Text = maskedTextBlock._provider.ToDisplayString();
        }

        private void MaskedTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            _provider = new MaskedTextProvider(InputMask, CultureInfo.CurrentCulture);
            _provider.Set(string.IsNullOrWhiteSpace(UnmaskedText) ? string.Empty : UnmaskedText);
            _provider.PromptChar = PromptChar;
            Text = _provider.ToDisplayString();
        }
    }

    public class MaskedTextBox : TextBox
    {
        public static readonly DependencyProperty IncludeLiteralsProperty = DependencyProperty.Register("IncludeLiterals", typeof(bool), typeof(MaskedTextBox), new UIPropertyMetadata(true, OnIncludeLiteralsPropertyChanged));

        public static readonly DependencyProperty IncludePromptProperty = DependencyProperty.Register("IncludePrompt", typeof(bool), typeof(MaskedTextBox), new UIPropertyMetadata(false, OnIncludePromptPropertyChanged));

        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register("Mask", typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata("<>", OnMaskPropertyChanged));

        public static readonly DependencyProperty PromptCharProperty = DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBox), new UIPropertyMetadata('_', OnPromptCharChanged));

        public static readonly DependencyProperty SelectAllOnGotFocusProperty = DependencyProperty.Register("SelectAllOnGotFocus", typeof(bool), typeof(MaskedTextBox), new PropertyMetadata(false));

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object>), typeof(MaskedTextBox));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(MaskedTextBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty ValueTypeProperty = DependencyProperty.Register("ValueType", typeof(Type), typeof(MaskedTextBox), new UIPropertyMetadata(typeof(string), OnValueTypeChanged));

        private bool _convertExceptionOccurred;

        private bool _isInitialized;

        /// <summary>
        ///     Flags if the Text and Value properties are in the process of being sync'd
        /// </summary>
        private bool _isSyncingTextAndValueProperties;

        static MaskedTextBox() => TextProperty.OverrideMetadata(typeof(MaskedTextBox), new FrameworkPropertyMetadata(OnTextChanged));

        public MaskedTextBox()
        {
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, Paste)); //handle paste
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, null, CanCut)); //surpress cut
        }

        public event RoutedPropertyChangedEventHandler<object> ValueChanged { add => AddHandler(ValueChangedEvent, value); remove => RemoveHandler(ValueChangedEvent, value); }

        public bool IncludeLiterals { get => (bool)GetValue(IncludeLiteralsProperty); set => SetValue(IncludeLiteralsProperty, value); }

        public bool IncludePrompt { get => (bool)GetValue(IncludePromptProperty); set => SetValue(IncludePromptProperty, value); }

        public string Mask { get => (string)GetValue(MaskProperty); set => SetValue(MaskProperty, value); }

        public char PromptChar { get => (char)GetValue(PromptCharProperty); set => SetValue(PromptCharProperty, value); }

        public bool SelectAllOnGotFocus { get => (bool)GetValue(SelectAllOnGotFocusProperty); set => SetValue(SelectAllOnGotFocusProperty, value); }

        public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public Type ValueType { get => (Type)GetValue(ValueTypeProperty); set => SetValue(ValueTypeProperty, value); }

        protected MaskedTextProvider MaskProvider { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateMaskProvider(Mask);
            UpdateText(0);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            if (Value == null || string.IsNullOrEmpty(Value.ToString()))
            {
                CaretIndex = 0;
            }

            if (SelectAllOnGotFocus)
            {
                SelectAll();
            }

            base.OnGotKeyboardFocus(e);
        }

        protected virtual void OnIncludeLiteralsChanged(bool oldValue, bool newValue) => UpdateMaskProvider(Mask);

        protected virtual void OnIncludePromptChanged(bool oldValue, bool newValue) => UpdateMaskProvider(Mask);

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            if (!_isInitialized)
            {
                _isInitialized = true;
                SyncTextAndValueProperties(ValueProperty, Value);
            }
        }

        protected virtual void OnMaskChanged(string oldValue, string newValue)
        {
            UpdateMaskProvider(newValue);
            UpdateText(0);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!e.Handled)
            {
                HandlePreviewKeyDown(e);
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (!e.Handled)
            {
                HandlePreviewTextInput(e);
            }

            base.OnPreviewTextInput(e);
        }

        protected virtual void OnPromptCharChanged(char oldValue, char newValue) => UpdateMaskProvider(Mask);

        protected virtual void OnTextChanged(string oldValue, string newValue)
        {
            if (_isInitialized)
            {
                SyncTextAndValueProperties(TextProperty, newValue);
            }
        }

        protected virtual void OnValueChanged(object oldValue, object newValue)
        {
            if (_isInitialized)
            {
                SyncTextAndValueProperties(ValueProperty, newValue);
            }

            RoutedPropertyChangedEventArgs<object> args = new RoutedPropertyChangedEventArgs<object>(oldValue, newValue) { RoutedEvent = ValueChangedEvent };
            RaiseEvent(args);
        }

        protected virtual void OnValueTypeChanged(Type oldValue, Type newValue)
        {
            if (_isInitialized)
            {
                SyncTextAndValueProperties(TextProperty, Text);
            }
        }

        private static void OnIncludeLiteralsPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = o as MaskedTextBox;
            maskedTextBox?.OnIncludeLiteralsChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        private static void OnIncludePromptPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = o as MaskedTextBox;
            maskedTextBox?.OnIncludePromptChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        private static void OnMaskPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = o as MaskedTextBox;
            maskedTextBox?.OnMaskChanged((string)e.OldValue, (string)e.NewValue);
        }

        private static void OnPromptCharChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = o as MaskedTextBox;
            maskedTextBox?.OnPromptCharChanged((char)e.OldValue, (char)e.NewValue);
        }

        private static void OnTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox inputBase = o as MaskedTextBox;
            inputBase?.OnTextChanged((string)e.OldValue, (string)e.NewValue);
        }

        private static void OnValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = o as MaskedTextBox;
            maskedTextBox?.OnValueChanged(e.OldValue, e.NewValue);
        }

        private static void OnValueTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MaskedTextBox maskedTextBox = o as MaskedTextBox;
            maskedTextBox?.OnValueTypeChanged((Type)e.OldValue, (Type)e.NewValue);
        }

        private void CanCut(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private object ConvertTextToValue()
        {
            object convertedValue = null;
            Type dataType = ValueType;
            string valueToConvert = MaskProvider.ToString().Trim();
            try
            {
                if (valueToConvert.GetType() == dataType || dataType.IsInstanceOfType(valueToConvert))
                {
                    convertedValue = valueToConvert;
                }
                else if (string.IsNullOrWhiteSpace(valueToConvert))
                {
                    convertedValue = Activator.CreateInstance(dataType);
                }
                else if (string.IsNullOrEmpty(valueToConvert))
                {
                    convertedValue = Activator.CreateInstance(dataType);
                }
                else if (convertedValue == null && valueToConvert is IConvertible)
                {
                    convertedValue = Convert.ChangeType(valueToConvert, dataType);
                }
            }
            catch
            {
                //if an excpetion occurs revert back to original value
                _convertExceptionOccurred = true;
                return Value;
            }

            return convertedValue;
        }

        private string ConvertValueToText(object value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (_convertExceptionOccurred)
            {
                value = Value;
                _convertExceptionOccurred = false;
            }

            //I have only seen this occur while in Blend, but we need it here so the Blend designer doesn't crash.
            if (MaskProvider == null)
            {
                return value.ToString();
            }

            MaskProvider.Set(value.ToString());
            return MaskProvider.ToDisplayString();
        }

        private int GetNextCharacterPosition(int startPosition)
        {
            int position = MaskProvider.FindEditPositionFrom(startPosition, true);
            return position == -1 ? startPosition : position;
        }

        private bool HandleKeyDownBack()
        {
            ModifierKeys modifiers = Keyboard.Modifiers;
            bool handled = true;
            if (modifiers == ModifierKeys.None || modifiers == ModifierKeys.Shift)
            {
                if (!RemoveSelectedText())
                {
                    int position = SelectionStart;
                    if (position > 0)
                    {
                        int newPosition = position - 1;
                        RemoveText(newPosition, 1);
                        UpdateText(newPosition);
                    }
                }
                else
                {
                    UpdateText();
                }
            }
            else if (modifiers == ModifierKeys.Control)
            {
                if (!RemoveSelectedText())
                {
                    RemoveTextFromStart(SelectionStart);
                    UpdateText(0);
                }
                else
                {
                    UpdateText();
                }
            }
            else
            {
                handled = false;
            }

            return handled;
        }

        private bool HandleKeyDownDelete()
        {
            ModifierKeys modifiers = Keyboard.Modifiers;
            bool handled = true;
            if (modifiers == ModifierKeys.None)
            {
                if (!RemoveSelectedText())
                {
                    int position = SelectionStart;
                    if (position < Text.Length)
                    {
                        RemoveText(position, 1);
                        UpdateText(position);
                    }
                }
                else
                {
                    UpdateText();
                }
            }
            else if (modifiers == ModifierKeys.Control)
            {
                if (!RemoveSelectedText())
                {
                    int position = SelectionStart;
                    RemoveTextToEnd(position);
                    UpdateText(position);
                }
                else
                {
                    UpdateText();
                }
            }
            else if (modifiers == ModifierKeys.Shift)
            {
                if (RemoveSelectedText())
                {
                    UpdateText();
                }
                else
                {
                    handled = false;
                }
            }
            else
            {
                handled = false;
            }

            return handled;
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                e.Handled = IsReadOnly || HandleKeyDownDelete();
            }
            else if (e.Key == Key.Back)
            {
                e.Handled = IsReadOnly || HandleKeyDownBack();
            }
            else if (e.Key == Key.Space)
            {
                if (!IsReadOnly)
                {
                    InsertText(" ");
                }

                e.Handled = true;
            }
            else if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                if (!IsReadOnly && AcceptsReturn)
                {
                    InsertText("\r");
                }

                // We don't want the OnPreviewTextInput to be triggered for the Return/Enter key
                // when it is not accepted.
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // We don't want the OnPreviewTextInput to be triggered at all for the Escape key.
                e.Handled = true;
            }
            else if (e.Key == Key.Tab)
            {
                if (AcceptsTab)
                {
                    if (!IsReadOnly)
                    {
                        InsertText("\t");
                    }

                    e.Handled = true;
                }
            }
        }

        private void HandlePreviewTextInput(TextCompositionEventArgs e)
        {
            if (!IsReadOnly)
            {
                InsertText(e.Text);
            }

            e.Handled = true;
        }

        private void InsertText(string text)
        {
            int position = SelectionStart;
            MaskedTextProvider provider = MaskProvider;
            bool textRemoved = RemoveSelectedText();
            position = GetNextCharacterPosition(position);
            if (!textRemoved && Keyboard.IsKeyToggled(Key.Insert))
            {
                if (provider.Replace(text, position))
                {
                    position += text.Length;
                }
            }
            else
            {
                if (provider.InsertAt(text, position))
                {
                    position += text.Length;
                }
            }

            position = GetNextCharacterPosition(position);
            UpdateText(position);
        }

        private void Paste(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly)
            {
                return;
            }

            object data = Clipboard.GetData(DataFormats.Text);
            if (data != null)
            {
                string text = data.ToString().Trim();
                if (text.Length > 0)
                {
                    int position = SelectionStart;
                    MaskProvider.Set(text);
                    UpdateText(position);
                }
            }
        }

        private bool RemoveSelectedText()
        {
            int length = SelectionLength;
            if (length == 0)
            {
                return false;
            }

            int position = SelectionStart;
            return MaskProvider.RemoveAt(position, position + length - 1);
        }

        private void RemoveText(int position, int length)
        {
            if (length == 0)
            {
                return;
            }

            MaskProvider.RemoveAt(position, position + length - 1);
        }

        private void RemoveTextFromStart(int endPosition) => RemoveText(0, endPosition);

        private void RemoveTextToEnd(int startPosition) => RemoveText(startPosition, Text.Length - startPosition);

        private void SyncTextAndValueProperties(DependencyProperty p, object newValue)
        {
            //prevents recursive syncing properties
            if (_isSyncingTextAndValueProperties)
            {
                return;
            }

            _isSyncingTextAndValueProperties = true;

            //this only occures when the user typed in the value
            if (TextProperty == p && newValue != null)
            {
                SetValue(ValueProperty, ConvertTextToValue());
            }

            SetValue(TextProperty, ConvertValueToText(newValue));
            _isSyncingTextAndValueProperties = false;
        }

        private void UpdateMaskProvider(string mask)
        {
            //do not create a mask provider if the Mask is empty, which can occur if the IncludePrompt and IncludeLiterals properties
            //are set prior to the Mask.
            if (string.IsNullOrEmpty(mask))
            {
                return;
            }

            MaskProvider = new MaskedTextProvider(mask)
            {
                IncludePrompt = IncludePrompt,
                IncludeLiterals = IncludeLiterals,
                PromptChar = PromptChar,
                ResetOnSpace = false //should make this a property
            };
        }

        private void UpdateText() => UpdateText(SelectionStart);

        private void UpdateText(int position)
        {
            MaskedTextProvider provider = MaskProvider;
            if (provider == null)
            {
                throw new InvalidOperationException();
            }

            Text = provider.ToDisplayString();
            SelectionLength = 0;
            SelectionStart = position;
        }
    }

    //public class MaskedTextBox : TextBox
    //{
    //    private MaskedTextProvider _provider;

    //    public MaskedTextBox()
    //    {
    //        Loaded += MaskedTextBox_Loaded;
    //        PreviewTextInput += MaskedTextBox_PreviewTextInput;
    //        PreviewKeyDown += MaskedTextBox_PreviewKeyDown;
    //    }

    //    private int GetNextCharacterPosition(int startPosition, bool goForward)
    //    {
    //        var position = _provider.FindEditPositionFrom(startPosition, goForward);
    //        if (position == -1) return startPosition;
    //        return position;
    //    }

    //    private void MaskedTextBox_Loaded(object sender, RoutedEventArgs e)
    //    {
    //        _provider = new MaskedTextProvider(InputMask, CultureInfo.CurrentCulture);
    //        _provider.Set(string.IsNullOrWhiteSpace(UnmaskedText) ? string.Empty : UnmaskedText);
    //        _provider.PromptChar = PromptChar;
    //        Text = _provider.ToDisplayString();
    //        var textProp = DependencyPropertyDescriptor.FromProperty(TextProperty, typeof(MaskedTextBox));
    //        textProp?.AddValueChanged(this, (s, args) => UpdateText());
    //        DataObject.AddPastingHandler(this, Pasting);
    //    }

    //    private void MaskedTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    //    {
    //        if (IsReadOnly) return;
    //        if (e.Key == Key.Space)
    //        {
    //            TreatSelectedText();
    //            var position = GetNextCharacterPosition(SelectionStart, true);
    //            if (_provider.InsertAt(" ", position)) RefreshText(position);
    //            e.Handled = true;
    //        }

    //        if (e.Key == Key.Back) //handle the back space
    //        {
    //            TreatSelectedText();
    //            var position = GetNextCharacterPosition(SelectionStart - 1, false);
    //            if (SelectionStart > 0 && _provider.RemoveAt(position)) position = GetNextCharacterPosition(position, false);
    //            if (position >= 0) RefreshText(position);
    //            e.Handled = true;
    //        }

    //        if (e.Key == Key.Delete)
    //        {
    //            if (TreatSelectedText())
    //            {
    //                RefreshText(SelectionStart);
    //            }
    //            else
    //            {
    //                if (_provider.RemoveAt(SelectionStart)) RefreshText(SelectionStart);
    //            }

    //            e.Handled = true;
    //        }
    //    }

    //    private void MaskedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    //    {
    //        if (IsReadOnly) return;
    //        TreatSelectedText();
    //        var position = GetNextCharacterPosition(SelectionStart, true);
    //        if (Keyboard.IsKeyToggled(Key.Insert))
    //        {
    //            if (_provider.Replace(e.Text, position)) position++;
    //        }
    //        else
    //        {
    //            if (_provider.InsertAt(e.Text, position)) position++;
    //        }

    //        position = GetNextCharacterPosition(position, true);
    //        RefreshText(position);
    //        e.Handled = true;
    //    }

    //    private void Pasting(object sender, DataObjectPastingEventArgs e)
    //    {
    //        if (e.DataObject.GetDataPresent(typeof(string)))
    //        {
    //            if (IsReadOnly) return;
    //            var pastedText = (string)e.DataObject.GetData(typeof(string));
    //            TreatSelectedText();
    //            var position = GetNextCharacterPosition(SelectionStart, true);
    //            if (_provider.InsertAt(pastedText, position)) RefreshText(position);
    //        }

    //        e.CancelCommand();
    //    }

    //    private void RefreshText(int position)
    //    {
    //        SetText(_provider.ToDisplayString(), _provider.ToString(false, false));
    //        SelectionStart = position;
    //    }

    //    private void SetText(string text, string unmaskedText)
    //    {
    //        UnmaskedText = string.IsNullOrWhiteSpace(unmaskedText) ? null : unmaskedText;
    //        Text = string.IsNullOrWhiteSpace(text) ? null : text;
    //    }

    //    private bool TreatSelectedText()
    //    {
    //        if (SelectionLength > 0) return _provider.RemoveAt(SelectionStart, SelectionStart + SelectionLength - 1);
    //        return false;
    //    }

    //    private void UpdateText()
    //    {
    //        if (_provider.ToDisplayString().Equals(Text)) return;
    //        var success = _provider.Set(Text);
    //        SetText(success ? _provider.ToDisplayString() : Text, _provider.ToString(false, false));
    //    }

    //    #region DependencyProperties

    //    public static readonly DependencyProperty InputMaskProperty = DependencyProperty.Register("InputMask", typeof(string), typeof(MaskedTextBox), null);

    //    public static readonly DependencyProperty PromptCharProperty = DependencyProperty.Register("PromptChar", typeof(char), typeof(MaskedTextBox), new PropertyMetadata('_'));

    //    public static readonly DependencyProperty UnmaskedTextProperty = DependencyProperty.Register("UnmaskedText", typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata(string.Empty,Changed));

    //    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        MaskedTextBox maskedtextBox = d as MaskedTextBox;
    //        maskedtextBox._provider = new MaskedTextProvider(maskedtextBox.InputMask, CultureInfo.CurrentCulture);
    //        maskedtextBox._provider.Set(string.IsNullOrWhiteSpace(maskedtextBox.UnmaskedText) ? string.Empty : e.NewValue as string);
    //        maskedtextBox.Text = maskedtextBox._provider.ToDisplayString();
    //    }

    //    public string InputMask
    //    {
    //        get => (string)GetValue(InputMaskProperty);
    //        set => SetValue(InputMaskProperty, value);
    //    }

    //    public char PromptChar
    //    {
    //        get => (char)GetValue(PromptCharProperty);
    //        set => SetValue(PromptCharProperty, value);
    //    }

    //    public string UnmaskedText
    //    {
    //        get => (string)GetValue(UnmaskedTextProperty);
    //        set => SetValue(UnmaskedTextProperty, value);
    //    }

    //    #endregion DependencyProperties
    //}
}