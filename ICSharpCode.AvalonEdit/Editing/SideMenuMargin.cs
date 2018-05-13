using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace ICSharpCode.AvalonEdit.Editing
{
  public class SideMenuMargin : AbstractMargin, IWeakEventListener
  {
    static SideMenuMargin()
    {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(SideMenuMargin),
                                               new FrameworkPropertyMetadata(typeof(SideMenuMargin)));

				}

	  public static readonly DependencyProperty EditIconProperty =
		  DependencyProperty.Register("EditIcon", typeof(Geometry), typeof(SideMenuMargin),
			  new FrameworkPropertyMetadata(OnEditIconChanged));
	  public Geometry EditIcon
				{
		  get { return (Geometry)GetValue(EditIconProperty); }
		  set { SetValue(EditIconProperty, value); }
	  }
	  static void OnEditIconChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
	  {

	  }
	  public static readonly DependencyProperty ShowIconProperty =
		  DependencyProperty.Register("ShowIcon", typeof(bool), typeof(SideMenuMargin),
			  new FrameworkPropertyMetadata(false));
	  public bool ShowIcon
				{
		  get { return (bool)GetValue(ShowIconProperty); }
		  set { SetValue(ShowIconProperty, value); }
	  }

	  public static readonly DependencyProperty IconPositionProperty =
		  DependencyProperty.Register("IconPosition", typeof(Point), typeof(SideMenuMargin));

	  public Point IconPosition
	  {
		  get { return (Point) GetValue(IconPositionProperty); }
		  set
		  {
			  SetValue(IconPositionProperty, value);
			 // InvalidateVisual();
		  }
	  }

	  public Action IconClicked;
				TextArea textArea;
	 
    /// <summary>
    /// The typeface used for rendering the line number margin.
    /// This field is calculated in MeasureOverride() based on the FontFamily etc. properties.
    /// </summary>
    protected Typeface typeface;

    /// <summary>
    /// The font size used for rendering the line number margin.
    /// This field is calculated in MeasureOverride() based on the FontFamily etc. properties.
    /// </summary>
    protected double emSize;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
      typeface = this.CreateTypeface();
      emSize = (double)GetValue(TextBlock.FontSizeProperty);

      FormattedText text = TextFormatterFactory.CreateFormattedText(
        this,
        new string('9', maxLineNumberLength),
        typeface,
        emSize,
        (Brush)GetValue(Control.ForegroundProperty)
      );
      return new Size(text.Width, 0);
    }

	  private Rect iconRect = new Rect();

	  /// <inheritdoc/>
	  protected override void OnRender(DrawingContext drawingContext)
	  {

		  TextView textView = this.TextView;
		  Size renderSize = this.RenderSize;
		  if (textView != null && textView.VisualLinesValid)
		  {


			  var visualLine = textView.GetVisualLine(textView.HighlightedLine);
			  if (visualLine == null) return;

			  double y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
			  IconPosition = new Point(renderSize.Width / 2, y - textView.VerticalOffset);
			  if (!ShowIcon)
				  return;
			  //var foreground = (Brush) GetValue(Control.ForegroundProperty);


			  //// int lineNumber = visualLine.FirstDocumentLine.LineNumber;
			  //FormattedText text = TextFormatterFactory.CreateFormattedText(
			  // this,
			  // "*",
			  // typeface, emSize, foreground
			  //);


			  //		drawingContext.DrawText(text, pos);

			  if (EditIcon != null)
			  {
				  //double centerX = pos.X + circlePath.Bounds.Width / 2;
				  //double pWidth = EditIcon.Bounds.Width / 2;
				  //double centerY = circlePath.Bounds.Top + circlePath.Bounds.Height / 2;
				  //double pHeight = EditIcon.Bounds.Height / 2;
				  iconRect = new Rect((int) IconPosition.X - 4, (int) IconPosition.Y + 2, 15, 13);
				  //drawingContext.DrawRectangle(Brushes.Red,null,r);
				  Matrix m = new Matrix();
				  m.Scale(0.35, 0.35);
				  m.Translate(IconPosition.X - 10, IconPosition.Y - 6);

				  //  TranslateTransform translateTransform = new TranslateTransform(pos.X, pos.Y);
				  MatrixTransform transform = new MatrixTransform(m);
				  drawingContext.PushTransform(transform);

				  drawingContext.DrawGeometry(Brushes.WhiteSmoke, null, EditIcon);
			  }
		  }
	  }

	  /// <inheritdoc/>
    protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
    {
      if (oldTextView != null)
      {
        oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
      }
      base.OnTextViewChanged(oldTextView, newTextView);
      if (newTextView != null)
      {
        newTextView.VisualLinesChanged += TextViewVisualLinesChanged;

        // find the text area belonging to the new text view
        textArea = newTextView.GetService(typeof(TextArea)) as TextArea;
      }
      else
      {
        textArea = null;
      }
      InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override void OnDocumentChanged(TextDocument oldDocument, TextDocument newDocument)
    {
      if (oldDocument != null)
      {
        PropertyChangedEventManager.RemoveListener(oldDocument, this, "LineCount");
      }
      base.OnDocumentChanged(oldDocument, newDocument);
      if (newDocument != null)
      {
        PropertyChangedEventManager.AddListener(newDocument, this, "LineCount");
      }
      OnDocumentLineCountChanged();
    }

    /// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
    protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
      if (managerType == typeof(PropertyChangedEventManager))
      {
        OnDocumentLineCountChanged();
        return true;
      }
      return false;
    }

    bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
    {
      return ReceiveWeakEvent(managerType, sender, e);
    }

    /// <summary>
    /// Maximum length of a line number, in characters
    /// </summary>
    protected int maxLineNumberLength = 1;

    void OnDocumentLineCountChanged()
    {
      int documentLineCount = Document != null ? Document.LineCount : 1;
      int newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

      // The margin looks too small when there is only one digit, so always reserve space for
      // at least two digits
      if (newLength < 2)
        newLength = 2;

      if (newLength != maxLineNumberLength)
      {
        maxLineNumberLength = newLength;
        InvalidateMeasure();
      }
    }

    void TextViewVisualLinesChanged(object sender, EventArgs e)
    {
      InvalidateVisual();
    }

    AnchorSegment selectionStart;
    bool selecting;

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
      base.OnMouseLeftButtonDown(e);
      if (!e.Handled && TextView != null && textArea != null && ShowIcon)
      {
        e.Handled = true;
        textArea.Focus();

	      var pos = e.GetPosition(this);
	      if (iconRect.Contains(pos))
	      {
		      if (IconClicked != null)
			      IconClicked();
	      }
					
      }
    }

    SimpleSegment GetTextLineSegment(MouseEventArgs e)
    {
      Point pos = e.GetPosition(TextView);
      pos.X = 0;
      pos.Y = pos.Y.CoerceValue(0, TextView.ActualHeight);
      pos.Y += TextView.VerticalOffset;
      VisualLine vl = TextView.GetVisualLineFromVisualTop(pos.Y);
      if (vl == null)
        return SimpleSegment.Invalid;
      TextLine tl = vl.GetTextLineByVisualYPosition(pos.Y);
      int visualStartColumn = vl.GetTextLineVisualStartColumn(tl);
      int visualEndColumn = visualStartColumn + tl.Length;
      int relStart = vl.FirstDocumentLine.Offset;
      int startOffset = vl.GetRelativeOffset(visualStartColumn) + relStart;
      int endOffset = vl.GetRelativeOffset(visualEndColumn) + relStart;
      if (endOffset == vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length)
        endOffset += vl.LastDocumentLine.DelimiterLength;
      return new SimpleSegment(startOffset, endOffset - startOffset);
    }

    void ExtendSelection(SimpleSegment currentSeg)
    {
      if (currentSeg.Offset < selectionStart.Offset)
      {
        textArea.Caret.Offset = currentSeg.Offset;
        textArea.Selection = Selection.Create(textArea, currentSeg.Offset, selectionStart.Offset + selectionStart.Length);
      }
      else
      {
        textArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
        textArea.Selection = Selection.Create(textArea, selectionStart.Offset, currentSeg.Offset + currentSeg.Length);
      }
    }

    /// <inheritdoc/>
    protected override void OnMouseMove(MouseEventArgs e)
    {
      if (selecting && textArea != null && TextView != null && ShowIcon)
      {
        e.Handled = true;
	
        //SimpleSegment currentSeg = GetTextLineSegment(e);
        //if (currentSeg == SimpleSegment.Invalid)
        //  return;
        //ExtendSelection(currentSeg);
        //textArea.Caret.BringCaretToView();
      }
      base.OnMouseMove(e);
    }

    /// <inheritdoc/>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
      if (selecting)
      {
        selecting = false;
        selectionStart = null;
        ReleaseMouseCapture();
        e.Handled = true;
      }
      base.OnMouseLeftButtonUp(e);
    }

    /// <inheritdoc/>
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
      // accept clicks even when clicking on the background
      return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }
  }
}
