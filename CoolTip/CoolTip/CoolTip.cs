using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel.Design;

namespace CoolTip
{
    [ProvideProperty("TipText", typeof(Control))]
    [ProvideProperty("HelpText", typeof(Control))]
    //[ToolboxItemFilter("System.Windows.Forms")]
    [DesignerCategory("CoolTip")]
    public class CoolTip : Component, IExtenderProvider, IMessageFilter
    {
        private class TipNativeWindow : NativeWindow
        {
            private CoolTip _control;

            internal TipNativeWindow(CoolTip control)
            {
                _control = control;
            }

            protected override void WndProc(ref Message m)
            {
                if (_control != null)
                    _control.WndProc(ref m);
            }
        }

        private class TipTimer : Timer
        {
            public object Target { get; }

            public TipTimer(object target)
            {
                Target = target;
            }
        }

        private TipNativeWindow _window;
        private TipTimer _timerHide;
        private TipTimer _timerShow;
        private Form _baseForm;
        private Control _topControl;
        private bool _isDisposing;

        private object _currentTarget;
        private object _hoverTarget;
        private object _manualTarget;
        private object _lastTarget;

        private HashSet<object> _targets;
        private RenderTipInfo _renderInfo;
        private DateTime _lastShowed;

        private Dictionary<Type, IManager> _managers;
        private ManagerControl _managerControl;
        private ManagerToolStripItem _managerToolStripItem;

        private Dictionary<Type, IVisitor> _visitors;
        private VisitorControl _visitorControl;
        private VisitorToolStrip _visitorToolStrip;
        private VisitorStatusStrip _visitorStatusStrip;

        public CoolTip(IContainer container)
            : this()
        {
            if (container == null)
                throw new ArgumentNullException("cont");

            container.Add(this);
        }

        public CoolTip()
        {
            ShowDelay = 1000;
            HideDelay = 5000;
            ReshowDelay = 100;
            _lastShowed = DateTime.MinValue;
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.ControlText;
            Marging = new Padding(0, 2, 0, 2);
            Padding = new Padding(2, 2, 2, 2);
            IconSize = new Size(12, 12);
            IconMarging = new Padding(2, 3, 2, 2);
            BorderWidth = 1;
            BorderColor = SystemColors.ActiveBorder;
            //_font = new Font("Arial", 9, FontStyle.Bold);
            Font = new Font("Microsoft Sans Serif", 8.25f);

            _targets = new HashSet<object>();
            _managers = new Dictionary<Type, IManager>();
            _visitors = new Dictionary<Type, IVisitor>();

            _managerControl = new ManagerControl();
            _managerToolStripItem = new ManagerToolStripItem();

            _managers.Add(typeof(Control), _managerControl);
            _managers.Add(typeof(ToolStripItem), _managerToolStripItem);

            _visitorControl = new VisitorControl();
            _visitorToolStrip = new VisitorToolStrip();
            _visitorStatusStrip = new VisitorStatusStrip();
            _visitors.Add(typeof(SplitContainer), _visitorControl);
            _visitors.Add(typeof(SplitterPanel), _visitorControl);
            _visitors.Add(typeof(Panel), _visitorControl);
            _visitors.Add(typeof(ToolStrip), _visitorToolStrip);
            _visitors.Add(typeof(StatusStrip), _visitorStatusStrip);

            _window = new TipNativeWindow(this);
        }

        ~CoolTip()
        {
            DestroyHandle();
        }

        private Control TopLevelControl
        {
            get
            {
                if (_topControl == null)
                    UpdateTopLevelControl();
                return _topControl;
            }
        }

        private Control UpdateTopLevelControl()
        {
            if (_topControl != null)
                return _topControl;

            Control baseVar = _baseForm;
            if (baseVar == null)
            {
                var controls = _targets
                    .Where(t => t.GetType().IsSubclassOf(typeof(Control)))
                    .Cast<Control>().ToArray();
                for (int index = 0; index < controls.Length; ++index)
                {
                    var control = controls[index];
                    baseVar = control.TopLevelControl;
                    if (baseVar != null)
                    {
                        break;
                    }

                    if (control is SplitContainer)
                    {
                        baseVar = control;
                        break;
                    }

                    // In designer, baseVar can be null since the Parent is not a TopLevel control
                    if (baseVar == null)
                    {
                        if (control != null && control.Parent != null)
                        {
                            while (control.Parent != null)
                            {
                                control = control.Parent;
                            }
                            baseVar = control;
                            if (baseVar != null)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            _topControl = baseVar;
            if (_topControl != null)
            {
                _topControl.HandleCreated += TopLevelCreated;
                _topControl.HandleDestroyed += TopLevelDestroyed;
                if (_topControl.IsHandleCreated)
                    TopLevelCreated(baseVar, EventArgs.Empty);
                baseVar.ParentChanged += TopLevelPropertyChanged;

                if (_baseForm == null)
                {
                    _baseForm = _topControl.FindForm();
                }

                if (_baseForm != null)
                {
                    // remove events to ensure only one handler
                    _baseForm.Deactivate -= ResetHint;
                    _baseForm.Move -= ResetHint;
                    _baseForm.Resize -= ResetHint;
                    // assign event back
                    _baseForm.Deactivate += ResetHint;
                    _baseForm.Move += ResetHint;
                    _baseForm.Resize += ResetHint;
                }

                if (!DesignMode)
                {
                    // ensure only one filter per instance
                    Application.RemoveMessageFilter(this);
                    Application.AddMessageFilter(this);
                }
            }

            return _topControl;
        }

        [Browsable(false)]
        public IntPtr Handle
        {
            get
            {
                if (!DesignMode && !IsHandleCreated())
                    CreateHandle();
                return _window.Handle;
            }
        }

        // Arial, 9pt, style=Bold
        [DefaultValue(typeof(Font), "Microsoft Sans Serif; 8,25pt")]
        public Font Font { get; set; }
             
        [DefaultValue(KnownColor.Window)]
        public Color BackColor { get; set; }

        [DefaultValue(KnownColor.ControlText)]
        public Color ForeColor { get; set; }

        [DefaultValue(1000)]
        public int ShowDelay { get; set; }

        [DefaultValue(5000)]
        public int HideDelay { get; set; }

        [DefaultValue(100)]
        public int ReshowDelay { get; set; }

        [DefaultValue(typeof(Padding), "0; 2; 0; 2")]
        public Padding Marging { get; set; }
        
        [DefaultValue(typeof(Padding), "2; 2; 2; 2")]
        public Padding Padding { get; set; }

        [DefaultValue(typeof(Size), "12; 12")]
        public Size IconSize { get; set; }

        [DefaultValue(typeof(Padding), "2; 3; 2; 2")]
        public Padding IconMarging { get; set; }

        [DefaultValue(1)]
        public int BorderWidth { get; set; }

        [DefaultValue(KnownColor.ActiveBorder)]
        public Color BorderColor { get; set; }

        [Browsable(true)]
        public Form BaseForm {
            get { return _baseForm; }
            set { _baseForm = value; UpdateTopLevelControl(); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposing = true;
                try
                {
                    ClearBaseFormEvent();
                    ClearTopLevelControlEvents();
                    StopTimerHide();
                    _window = null;
                    Application.RemoveMessageFilter(this);
                }
                finally
                {
                    _isDisposing = false;
                }
            }

            base.Dispose(disposing);
        }


        private void TopLevelCreated(object sender, EventArgs e)
        {
            CreateHandle();
        }

        private void CreateHandle()
        {
            if (!_isDisposing && IsHandleCreated() == false)
            {
                _window.CreateHandle(GetCreateParams());
            }
        }

        private void TopLevelDestroyed(object sender, EventArgs e)
        {
            DestroyHandle();
        }

        private void DestroyHandle()
        {
            if (!_isDisposing && IsHandleCreated())
            {
                _window.DestroyHandle();
            }
        }

        private void TopLevelPropertyChanged(object s, EventArgs e)
        {
            ClearBaseFormEvent();
            ClearTopLevelControlEvents();
            _topControl = null;

            // We must re-aquire this control.  If the existing top level control's handle
            // was never created, but the new parent has a handle, if we don't re-get
            // the top level control here we won't ever create the tooltip handle.
            _topControl = TopLevelControl;
        }

        private void ClearTopLevelControlEvents()
        {
            if (_topControl != null)
            {
                _topControl.ParentChanged -= TopLevelPropertyChanged;
                _topControl.HandleCreated -= TopLevelCreated;
                _topControl.HandleDestroyed -= TopLevelDestroyed;
            }
        }

        private void ClearBaseFormEvent()
        {
            if (_baseForm != null)
            {
                _baseForm.Deactivate -= ResetHint;
                _baseForm.Move -= ResetHint;
                _baseForm.Resize -= ResetHint;
            }
        }

        
        private void ResetHint(object sender, EventArgs e)
        {
            StopTimerShow();
            DoHide();
        }

        private void HandleCreated(object sender, EventArgs eventargs)
        {
            // Reset the toplevel control when the owner's handle is recreated.
            ClearTopLevelControlEvents();
            _topControl = null;
            UpdateTopLevelControl();
        }

        public bool CanExtend(object target)
        {
            return (target is Control) && !(target is CoolTip);
        }

        public void AddInfo<TComponent>(
            Func<TComponent, string> getTip,
            Func<TComponent, Rectangle> getBounds,
            Func<TComponent, bool> getVisible)
            where TComponent : Component
        {
            var info = new Manager<TComponent>();
            info.Tip = getTip;
            info.Bounds = getBounds;
            info.Visible = getVisible;
            _managers.Add(typeof(TComponent), info);
        }

        public IManager GetManager(object target)
        {
            var type = target.GetType();
            if (_managers.ContainsKey(type))
                return _managers[type];
            else
            {
                foreach (var pair in _managers)
                {
                    if (type.IsSubclassOf(pair.Key))
                        return pair.Value;
                }
                throw new IndexOutOfRangeException("target");
            }
        }

        private object FindCurrentTarget()
        {
            var location = _baseForm.PointToClient(Cursor.Position);
            object target = _baseForm.GetChildAtPoint(location);
            while ((target != null) && _visitors.Keys.Contains(target.GetType()))
            {
                var visitor = _visitors[target.GetType()];
                object newTarget = visitor.GetItem(target, Cursor.Position);

                if ((newTarget == null) || (target == newTarget))
                    break;

                target = newTarget;
                //Console.WriteLine(target);
            }
            return target;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (_baseForm == null || _isDisposing || (_baseForm.Handle != Native.GetForegroundWindow()))
                return false;

            if (m.Msg == Native.WM_MOUSEMOVE)
            {
                //Console.WriteLine(m);
                //Console.WriteLine(_baseForm);

                var target = FindCurrentTarget();
                if ((target != _hoverTarget) && (target != null))
                {
                    // stop previous show-by-timer
                    StopTimerShow();
                    // start new show-by-timer
                    ShowByTimer(target, ShowDelay);
                }
                if ((target != _lastTarget) && (DateTime.Now > _lastShowed.AddMilliseconds(ReshowDelay)))
                {
                    // reset last showed only after delay
                    _lastTarget = null;
                }
                if ((target != null) && (_manualTarget == null))
                {
                    if ((target != _currentTarget) && (_currentTarget != null))
                    {
                        // hide previous
                        DoHide();
                        // show new without additional delay
                        DoShow(target);
                    }
                    else if ((target != _lastTarget) && (_lastTarget != null))
                    {
                        // reshow new without additional delay
                        DoShow(target);
                    }
                }
            }
            else if (m.Msg == Native.WM_MOUSELEAVE)
            {
                if (_manualTarget == null)
                    DoHide();
                StopTimerShow();
            }
            else if (m.Msg == Native.WM_KEYDOWN)
            {
                DoHide();
                StopTimerShow();
            }

            return false;
        }

        public void SetTipText(Control control, string caption)
        {
            _targets.Add(control);
            bool added = _managerControl.SetTip(control, caption);
            if (!DesignMode)
            {
                if (added)
                {
                    // ensure only once HandleCreated
                    control.HandleCreated -= HandleCreated;
                    control.HandleCreated += HandleCreated;
                    // call immediately
                    if (control.IsHandleCreated)
                        HandleCreated(control, EventArgs.Empty);
                }
                else
                {
                    control.HandleCreated -= HandleCreated;
                }
                UpdateTopLevelControl();
            }
        }

        [DefaultValue("")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string GetTipText(Control control)
        {
            return _managerControl.GetTip(control);
        }

        public void SetHelpText(Control control, string help)
        {
            _targets.Add(control);
            bool added = _managerControl.SetHelp(control, help);
            if (!DesignMode)
            {
                if (added)
                {
                    // ensure only once HandleCreated
                    control.HandleCreated -= HandleCreated;
                    control.HandleCreated += HandleCreated;
                    // ensure only once HelpRequested
                    control.HelpRequested -= HelpRequested;
                    control.HelpRequested += HelpRequested;
                    // call immediately
                    if (control.IsHandleCreated)
                        HandleCreated(control, EventArgs.Empty);
                }
                else
                {
                    control.HandleCreated -= HandleCreated;
                }
                UpdateTopLevelControl();
            }
        }

        [DefaultValue("")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string GetHelpText(Control control)
        {
            return _managerControl.GetHelp(control);
        }

        public void Show(object target, Icon icon, int? delay, string text)
        {
            var manager = GetManager(target);
            if (!manager?.GetVisible(target) ?? false)
                return;

            DoHide();

            _manualTarget = target;
            _renderInfo = new RenderTipInfo(icon, text, delay);
            DoShow(target, manager);
            HideByTimer(target, delay ?? HideDelay);
        }

        public void Hide()
        {
            if (_manualTarget != null)
                DoHide();
        }

        private void HelpRequested(object sender, HelpEventArgs e)
        {
            DoHide();

            _manualTarget = sender;
            string text = _managerControl.GetHelp(sender);
            _renderInfo = new RenderTipInfo(Icon.Information, text, 0);
            DoShow(sender, _managerControl);
        }

        internal bool IsHandleCreated()
        {
            return (_window != null) && (_window.Handle != IntPtr.Zero);
        }

        protected virtual CreateParams GetCreateParams()
        {
            CreateParams cp = new CreateParams();
            if (TopLevelControl != null && !TopLevelControl.IsDisposed)
            {
                cp.Parent = TopLevelControl.Handle;
            }
            cp.ClassName = null;
            cp.Style = Native.WS_POPUP;
            cp.ExStyle = Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
            cp.Caption = null;

            return cp;
        }

        private void DoHide()
        {
            if (_currentTarget != null && !_isDisposing)
            {
                var handle = new HandleRef(this, Handle);
                Native.ShowWindow(handle, Native.SW_HIDE);
                StopTimerHide();
                _lastTarget = _currentTarget;
                _currentTarget = null;
                _renderInfo = null;
                _manualTarget = null;
                _lastShowed = DateTime.Now;
            }
        }

        private void HideByTimer(object target, int delay)
        {
            if ((_timerHide == null) && (delay > 0))
            {
                _timerHide = new TipTimer(target);
                _timerHide.Tick += TimerHideHandler;
                _timerHide.Interval = delay;
                _timerHide.Start();
            }
        }

        protected void StopTimerHide()
        {
            if (_timerHide != null)
            {
                _timerHide.Stop();
                _timerHide.Dispose();
                _timerHide = null;
            }
        }

        private void TimerHideHandler(object source, EventArgs args)
        {
            DoHide();
        }

        private void ShowByTimer(object target, int interval)
        {
            if (_timerShow == null)
            {
                _hoverTarget = target;
                _timerShow = new TipTimer(target);
                _timerShow.Tick += TimerShowHandler;
                _timerShow.Interval = interval;
                _timerShow.Start();
            }
        }

        protected void StopTimerShow()
        {
            if (_timerShow != null)
            {
                _timerShow.Stop();
                _timerShow.Dispose();
                _timerShow = null;
            }
        }

        private void TimerShowHandler(object source, EventArgs args)
        {
            var timer = source as TipTimer;
            if (_baseForm.Bounds.Contains(Cursor.Position))
                DoShow(timer.Target);
        }

        private Size GetTextRect(string caption)
        {
            return TextRenderer.MeasureText(caption, Font);
        }

        private void DoShow(object target)
        {
            var manager = GetManager(target);
            if (!manager?.GetVisible(target) ?? false)
                return;

            string hint = manager.GetTip(target);
            if (String.IsNullOrWhiteSpace(hint))
                return;

            DoHide();

            _renderInfo = new RenderTipInfo(hint);
            DoShow(target, manager);
            HideByTimer(target, HideDelay);
        }

        private void DoShow(object target, IManager manager)
        {
            _currentTarget = target;

            var bounds = manager.GetBounds(target);
            var location = new Point(bounds.Left - Marging.Left, bounds.Bottom + Marging.Bottom);
            var border = new Padding(BorderWidth);
            var size = GetTextRect(_renderInfo.Info.Text) + Padding.Size + border.Size;
            var screen = Screen.FromRectangle(bounds).Bounds;
            var window = _baseForm.Bounds;

            size.Width += IconSize.Width + IconMarging.Horizontal;
            var rect = new Rectangle(location, size);

            bool isOutOfBottom = (rect.Bottom > window.Bottom)
                && (rect.Height < window.Height);
            if (isOutOfBottom)
            {
                rect.Y = bounds.Top - size.Height - Marging.Top - border.Horizontal;
                _renderInfo.MoveIconDown();
            }

            bool isOutOfRight = (rect.Right > window.Right);
            int futureLeft = bounds.Left - size.Width - Marging.Left - border.Horizontal;
            bool isOutOfLeft = (futureLeft < window.Left);
            bool widtherThanWindow = (rect.Width < window.Width);
            bool isOutOfScreen = (rect.Right > screen.Width);

            if ((isOutOfRight && !isOutOfLeft && widtherThanWindow) || isOutOfScreen)
            {
                rect.X = futureLeft;
                _renderInfo.MoveIconRight();
            }
            if (rect.Width * 2 < bounds.Width)
            {
                _renderInfo.RedirectArrowRight();
            }

            var handle = new HandleRef(this, Handle);
            Native.SetWindowPos(handle,
                Native.HWND_TOPMOST, rect.Left, rect.Top, rect.Width, rect.Height,
                Native.SWP_NOACTIVATE | Native.SWP_NOOWNERZORDER);
            Native.ShowWindow(handle, Native.SW_SHOWNOACTIVATE);
            Native.UpdateWindow(handle);
            StopTimerShow();
        }

        private void Draw(Graphics graphics, Rectangle bounds, RenderTipInfo info)
        {
            bounds.Width -= 1;
            bounds.Height -= 1;

            using (var brush = new SolidBrush(BackColor))
                graphics.FillRectangle(brush, bounds);
            using (var pen = new Pen(BorderColor, BorderWidth))
                graphics.DrawRectangle(pen, bounds);
            using (var brush = new SolidBrush(info.IconBackground))
            {
                var border = new Padding(BorderWidth);
                var text = GetTextRect(info.Info.Text);

                Rectangle rectangle = Rectangle.Empty;
                if (info.IsIconAtRight)
                {
                    rectangle = new Rectangle(
                        bounds.Left + BorderWidth + text.Width + Padding.Horizontal,
                        bounds.Top + BorderWidth,
                        IconSize.Width + IconMarging.Horizontal,
                        text.Height + Padding.Vertical);
                }
                else
                {
                    rectangle = new Rectangle(
                        bounds.Left + BorderWidth,
                        bounds.Top + BorderWidth,
                        IconSize.Width + IconMarging.Horizontal,
                        text.Height + Padding.Vertical);
                }
                graphics.FillRectangle(brush, rectangle);

                if (info.IsAtBottom)
                {
                    rectangle = new Rectangle(
                        rectangle.Left + IconMarging.Left,
                        bounds.Bottom - BorderWidth - IconMarging.Bottom - IconSize.Height,
                        IconSize.Width, IconSize.Height);
                }
                else
                {
                    rectangle = new Rectangle(
                        rectangle.Left + IconMarging.Left,
                        rectangle.Top + IconMarging.Top,
                        IconSize.Width, IconSize.Height);
                }
                var icon = info.GetIconBitmap();
                icon.MakeTransparent(Color.White);
                graphics.DrawImage(icon, rectangle);
            }
            var location = Point.Empty;
            if (info.IsIconAtRight)
            {
                location = new Point(
                    bounds.Left + BorderWidth + Padding.Left,
                    bounds.Top + BorderWidth + Padding.Top);
            }
            else
            {
                location = new Point(
                    bounds.Left + BorderWidth + IconMarging.Horizontal + IconSize.Width + Padding.Left,
                    bounds.Top + BorderWidth + Padding.Top);
            }
            TextRenderer.DrawText(graphics, info.Info.Text, Font, location, ForeColor);
        }

        private void WndProc(ref Message msg)
        {
            //Console.WriteLine(msg.ToString());
            switch (msg.Msg)
            {
                case Native.WM_PRINTCLIENT:
                case Native.WM_PAINT:
                    {
                        Native.PAINTSTRUCT lpPaint = default(Native.PAINTSTRUCT);
                        IntPtr hdc = Native.BeginPaint(new HandleRef(this, msg.HWnd), ref lpPaint);
                        Graphics graphics = Graphics.FromHdc(hdc);
                        try
                        {
                            Rectangle bounds = new Rectangle(
                                lpPaint.rcPaint_left, lpPaint.rcPaint_top,
                                lpPaint.rcPaint_right - lpPaint.rcPaint_left,
                                lpPaint.rcPaint_bottom - lpPaint.rcPaint_top);
                            if (bounds == Rectangle.Empty)
                                break;

                            Draw(graphics, bounds, _renderInfo);
                        }
                        finally
                        {
                            graphics.Dispose();
                            Native.EndPaint(new HandleRef(this, msg.HWnd), ref lpPaint);
                        }
                    }
                    break;
            }

            if (_window != null)
            {
                _window.DefWndProc(ref msg);
            }
        }
    }
}
