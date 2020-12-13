using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace CvProgram
{
    public class ItemLocation : INotifyPropertyChanged
    {
        private readonly UIElement _ItemContainer;

        private readonly Panel _Panel;

        private Point? _Location;

        private Point? _LocationN;

        public ItemLocation(Panel panel, UIElement itemContainer)
        {
            this._Panel = panel;
            this._ItemContainer = itemContainer;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Point? Location
        {
            get
            {
                if (_Location == null && _Panel != null && _ItemContainer != null)
                {
                    _Location = _ItemContainer.TranslatePoint(default, _Panel);
                }
                return _Location;
            }
        }

        public Point? LocationN
        {
            get
            {
                if (_LocationN == null && _Location == null && _Panel != null && _ItemContainer != null)
                {
                    Point? np = Location;
                    if (np != null)
                    {
                        _LocationN = new Point(-np.Value.X - 150, -np.Value.Y);
                    }
                }
                return _LocationN;
            }
        }

        internal void OnLocationPropertyChanged()
        {
            _Location = null;
            _LocationN = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocationN)));
        }
    }

    public class VirtualizingTilePanel : System.Windows.Controls.VirtualizingPanel, IScrollInfo
    {
        public static readonly DependencyProperty ChildHeightProperty
            = DependencyProperty.RegisterAttached("ChildHeight", typeof(double), typeof(VirtualizingTilePanel),
                new FrameworkPropertyMetadata(385d, FrameworkPropertyMetadataOptions.AffectsMeasure |
                                                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ChildWidthProperty
            = DependencyProperty.RegisterAttached("ChildWidth", typeof(double), typeof(VirtualizingTilePanel),
                new FrameworkPropertyMetadata(220d, FrameworkPropertyMetadataOptions.AffectsMeasure |
                                                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ItemLocationProperty = DependencyProperty.RegisterAttached("ItemLocation", typeof(ItemLocation), typeof(VirtualizingTilePanel), new PropertyMetadata(null));

        private int _columns;

        public VirtualizingTilePanel()
        {
            RenderTransform = _trans;
        }

        public double ChildHeight
        {
            get { return (double)GetValue(ChildHeightProperty); }
            set { SetValue(ChildHeightProperty, value); }
        }

        public double ChildWidth
        {
            get { return (double)GetValue(ChildWidthProperty); }
            set { SetValue(ChildWidthProperty, value); }
        }

        public static ItemLocation GetItemLocation(DependencyObject obj)
        {
            return (ItemLocation)obj.GetValue(ItemLocationProperty);
        }

        public static void SetItemLocation(DependencyObject obj, ItemLocation value)
        {
            obj.SetValue(ItemLocationProperty, value);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            try
            {
                IItemContainerGenerator generator = ItemContainerGenerator;
                UpdateScrollInfo(finalSize);
                for (int i = 0; i < Children.Count; i++)
                {
                    UIElement child = Children[i];

                    int itemIndex = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));

                    ArrangeChild(itemIndex, child, finalSize);

                    ItemLocation itemLocation = GetItemLocation(child);
                    if (itemLocation == null)
                    {
                        itemLocation = new ItemLocation(this, child);
                        SetItemLocation(child, itemLocation);
                    }
                    itemLocation.OnLocationPropertyChanged();
                }

                return finalSize;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                if (availableSize.Width == double.PositiveInfinity || availableSize.Height == double.PositiveInfinity)
                {
                    return Size.Empty;
                }

                _columns = (int)(availableSize.Width / ChildWidth);

                UpdateScrollInfo(availableSize);

                GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex);

                UIElementCollection children = InternalChildren;
                IItemContainerGenerator generator = ItemContainerGenerator;

                GeneratorPosition startPos = generator.GeneratorPositionFromIndex(firstVisibleItemIndex);

                int childIndex = (startPos.Offset == 0) ? startPos.Index : startPos.Index + 1;

                using (generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    for (int itemIndex = firstVisibleItemIndex;
                        itemIndex <= lastVisibleItemIndex;
                        ++itemIndex, ++childIndex)
                    {
                        if (generator.GenerateNext(out bool newlyRealized) is not UIElement child)
                        {
                            continue;
                        }

                        if (newlyRealized)
                        {
                            if (childIndex >= children.Count)
                            {
                                AddInternalChild(child);
                            }
                            else
                            {
                                InsertInternalChild(childIndex, child);
                            }
                            generator.PrepareItemContainer(child);
                        }

                        child.Measure(GetChildSize());
                    }
                }

                CleanUpItems(firstVisibleItemIndex, lastVisibleItemIndex);

                return availableSize;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            try
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Move:
                        RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void CleanUpItems(int minDesiredGenerated, int maxDesiredGenerated)
        {
            try
            {
                UIElementCollection children = InternalChildren;
                IItemContainerGenerator generator = ItemContainerGenerator;

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    GeneratorPosition childGeneratorPos = new GeneratorPosition(i, 0);
                    int itemIndex = generator.IndexFromGeneratorPosition(childGeneratorPos);
                    if (itemIndex < minDesiredGenerated || itemIndex > maxDesiredGenerated)
                    {
                        generator.Remove(childGeneratorPos, 1);
                        RemoveInternalChildRange(i, 1);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #region Layout specific code

        private void ArrangeChild(int itemIndex, UIElement child, Size finalSize)
        {
            try
            {
                int childrenPerRow = CalculateChildrenPerRow(finalSize);

                int row = itemIndex / childrenPerRow;
                int column = itemIndex % childrenPerRow;
                double columnWidth = Math.Floor(finalSize.Width / _columns);

                child.Arrange(new Rect(columnWidth * column, row * ChildHeight, columnWidth,
                    child.DesiredSize.Height));
            }
            catch (Exception)
            {
            }
        }

        private int CalculateChildrenPerRow(Size availableSize)
        {
            try
            {
                int childrenPerRow = double.IsPositiveInfinity(availableSize.Width)
                    ? Children.Count
                    : Math.Max(1, (int)Math.Floor(availableSize.Width / ChildWidth));
                return childrenPerRow;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Size CalculateExtent(Size availableSize, int itemCount)
        {
            try
            {
                int childrenPerRow = CalculateChildrenPerRow(availableSize);

                return new Size(childrenPerRow * ChildWidth,
                    ChildHeight * Math.Ceiling((double)itemCount / childrenPerRow));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Size GetChildSize()
        {
            return new Size(ChildWidth, ChildHeight);
        }

        private void GetVisibleRange(out int firstVisibleItemIndex, out int lastVisibleItemIndex)
        {
            try
            {
                int childrenPerRow = CalculateChildrenPerRow(_extent);

                firstVisibleItemIndex = (int)Math.Floor(_offset.Y / ChildHeight) * childrenPerRow;
                lastVisibleItemIndex =
                    (int)Math.Ceiling((_offset.Y + _viewport.Height) / ChildHeight) * childrenPerRow - 1;

                ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
                int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;
                if (lastVisibleItemIndex >= itemCount)
                {
                    lastVisibleItemIndex = itemCount - 1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion Layout specific code

        #region IScrollInfo implementation

        private readonly TranslateTransform _trans = new TranslateTransform();

        private Size _extent = new Size(0, 0);

        private Point _offset;

        private Size _viewport = new Size(0, 0);

        public bool CanHorizontallyScroll { get; set; } = false;

        public bool CanVerticallyScroll { get; set; } = false;

        public double ExtentHeight => _extent.Height;

        public double ExtentWidth => _extent.Width;

        public double HorizontalOffset => _offset.X;

        public ScrollViewer ScrollOwner { get; set; }

        public double VerticalOffset => _offset.Y;

        public double ViewportHeight => _viewport.Height;

        public double ViewportWidth => _viewport.Width;

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + 10);
        }

        public void LineLeft()
        {
            throw new InvalidOperationException();
        }

        public void LineRight()
        {
            throw new InvalidOperationException();
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - 10);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            return new Rect();
        }

        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + 10);
        }

        public void MouseWheelLeft()
        {
            throw new InvalidOperationException();
        }

        public void MouseWheelRight()
        {
            throw new InvalidOperationException();
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - 10);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + _viewport.Height);
        }

        public void PageLeft()
        {
            throw new InvalidOperationException();
        }

        public void PageRight()
        {
            throw new InvalidOperationException();
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - _viewport.Height);
        }

        public void SetHorizontalOffset(double offset)
        {
            throw new InvalidOperationException();
        }

        public void SetVerticalOffset(double offset)
        {
            try
            {
                if (offset < 0 || _viewport.Height >= _extent.Height)
                {
                    offset = 0;
                }
                else
                {
                    if (offset + _viewport.Height >= _extent.Height)
                    {
                        offset = _extent.Height - _viewport.Height;
                    }
                }

                _offset.Y = offset;

                ScrollOwner?.InvalidateScrollInfo();

                _trans.Y = -offset;

                InvalidateMeasure();
            }
            catch (Exception)
            {
            }
        }

        private void UpdateScrollInfo(Size availableSize)
        {
            try
            {
                ItemsControl itemsControl = ItemsControl.GetItemsOwner(this);
                int itemCount = itemsControl.HasItems ? itemsControl.Items.Count : 0;

                Size extent = CalculateExtent(availableSize, itemCount);
                if (extent != _extent)
                {
                    _extent = extent;
                    ScrollOwner?.InvalidateScrollInfo();
                }

                if (availableSize != _viewport)
                {
                    _viewport = availableSize;
                    ScrollOwner?.InvalidateScrollInfo();
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion IScrollInfo implementation
    }
}