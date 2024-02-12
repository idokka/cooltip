using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Collections;

namespace CoolTip
{
    /// <summary>
    /// Represents a small rectangular pop-up window that displays a brief description
    /// of a control's purpose when the user rests the pointer on the control.
    /// </summary>
    [ProvideProperty("TipText", typeof(Control))]
    [ProvideProperty("HelpText", typeof(Control))]
    [ProvideProperty("ShowLongItemTips", typeof(ListBox))]
    [ProvideProperty("ShowItemTips", typeof(ListView))]
    //[ToolboxItemFilter("System.Windows.Forms")]
    [DesignerCategory("CoolTip")]
    [ToolboxBitmap(typeof(ToolTip))]
    public class CoolTip : Component, IExtenderProvider, IMessageFilter
    {
        /// <summary>
        /// Wrapper for native window.
        /// </summary>
        private class TipNativeWindow : NativeWindow
        {
            /// <summary>
            /// Reference to the owner component.
            /// </summary>
            private CoolTip _control;

            /// <summary>
            /// Create new tip window and save reference to the owner component.
            /// </summary>
            /// <param name="control">Reference to the owner component.</param>
            internal TipNativeWindow(CoolTip control)
            {
                _control = control;
            }

            /// <summary>
            /// Pass window message process to the owner component.
            /// </summary>
            /// <param name="msg"></param>
            protected override void WndProc(ref Message msg)
            {
                if (_control != null)
                    _control.WndProc(ref msg);
            }
        }

        /// <summary>
        /// Universal timer, used to delay actions.
        /// </summary>
        private class TipTimer : Timer
        {
            /// <summary>
            /// Target for the delayed action.
            /// </summary>
            public object Target { get; }

            /// <summary>
            /// Create new timer and save target for delayed action.
            /// </summary>
            /// <param name="target"></param>
            public TipTimer(object target)
            {
                Target = target;
            }
        }

        /// <summary>
        /// Wrapper for tip native window.
        /// </summary>
        private TipNativeWindow _window;

        /// <summary>
        /// Timer for delayed window hide.
        /// </summary>
        private TipTimer _timerHide;

        /// <summary>
        /// Timer for delayed window show.
        /// </summary>
        private TipTimer _timerShow;

        /// <summary>
        /// Parent form.
        /// Needed to extend functionality and override default befavior.
        /// </summary>
        private Form _baseForm;

        /// <summary>
        /// Top-level control (in general case - base form).
        /// Ported from original `ToolTip`.
        /// </summary>
        private Control _topControl;

        /// <summary>
        /// Object currently in `disposing` state.
        /// </summary>
        private bool _isDisposing;

        /// <summary>
        /// Set to the target conrtol for currently showing tip window.
        /// Will be reset to `null` after tip window hide.
        /// </summary>
        private object _currentTarget;

        /// <summary>
        /// Target control for delayed tip show.
        /// Will be reset to `null` after delayed action process.
        /// </summary>
        private object _futureTarget;

        /// <summary>
        /// Target control for manual tip show.
        /// </summary>
        private object _manualTarget;

        /// <summary>
        /// Target control for later showed tip.
        /// Will be reset to `null` after `ReshowDelay` delay.
        /// </summary>
        private object _lastTarget;

        /// <summary>
        /// All available targets (i.e. with assigned tip text / help text) placed on the `BaseForm`.
        /// </summary>
        private HashSet<object> _targets;

        /// <summary>
        /// Render options for next (current) tool tip.
        /// Includes tip text, icon and window geometry.
        /// </summary>
        private RenderTipInfo _renderInfo;

        /// <summary>
        /// Timestamp of later showed tool tip.
        /// </summary>
        private DateTime _lastShowed;

        /// <summary>
        /// All available `ListBox`se for process their items.
        /// </summary>
        private HashSet<ListBox> _listBoxes;

        /// <summary>
        /// All available `ListView`s for process their items.
        /// </summary>
        private HashSet<ListView> _listViews;

        /// <summary>
        /// List of all assigned tip managers.
        /// Make relationship between type and it's manager.
        /// </summary>
        private Dictionary<Type, IManager> _managers;

        /// <summary>
        /// General tip manager, used for all controls derived from `Control`.
        /// </summary>
        private ManagerControl _managerControl;

        /// <summary>
        /// Tip manager for `ToolStripItem`s.
        /// </summary>
        private ManagerToolStripItem _managerToolStripItem;

        /// <summary>
        /// Tip manager for `ListBox` items.
        /// <seealso cref="ListBoxItem"/>.
        /// </summary>
        private ManagerListBoxItem _managerListBox;

        /// <summary>
        /// Tip manager for `ListViewItem`s.
        /// </summary>
        private ManagerListViewItem _managerListView;

        /// <summary>
        /// List of all assigned visitors.
        /// Make relationship between type and it's visitor.
        /// </summary>
        private Dictionary<Type, IVisitor> _visitors;

        /// <summary>
        /// General visitor for components, derived from `Control`.
        /// Used to process `Panel`, `GroupBox` etc.
        /// </summary>
        private VisitorControl _visitorControl;

        /// <summary>
        /// Visitor for `ToolStrip` component.
        /// Used to detect hovered `ToolStripItem` inside the hovered `ToolStrip`.
        /// </summary>
        private VisitorToolStrip _visitorToolStrip;

        /// <summary>
        /// Visitor for `StatusStrip` component.
        /// Used to detect hovered `ToolStripItem` inside the hovered `StatusStrip`.
        /// </summary>
        private VisitorStatusStrip _visitorStatusStrip;

        /// <summary>
        /// Visitor for `ListBox`.
        /// Used to detect hovered line inside the hovered `ListBox`.
        /// <seealso cref="ListBoxItem"/>.
        /// </summary>
        private VisitorListBox _visitorListBox;

        /// <summary>
        /// Visitor for `ListView`.
        /// Used to detect hovered `ListViewItem` inside the hovered `ListView`.
        /// </summary>
        private VisitorListView _visitorListView;

        /// <summary>
        /// Create new object and set it's owner container.
        /// </summary>
        /// <param name="container">Parent container</param>
        /// <exception cref="ArgumentNullException">Will be thrown if `container` is `null`.</exception>
        public CoolTip(IContainer container)
            : this()
        {
            if (container == null)
                throw new ArgumentNullException("cont");

            container.Add(this);
        }

        /// <summary>
        /// Create new object, initialize it's properties with default values.
        /// </summary>
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

            _listBoxes = new HashSet<ListBox>();
            _listViews = new HashSet<ListView>();

            _managerControl = new ManagerControl();
            _managerToolStripItem = new ManagerToolStripItem();
            _managerListBox = new ManagerListBoxItem();
            _managerListView = new ManagerListViewItem();

            _managers.Add(typeof(Control), _managerControl);
            _managers.Add(typeof(ToolStripItem), _managerToolStripItem);
            _managers.Add(typeof(ListBoxItem), _managerListBox);
            _managers.Add(typeof(ListViewItem), _managerListView);

            _visitorControl = new VisitorControl();
            _visitorToolStrip = new VisitorToolStrip();
            _visitorStatusStrip = new VisitorStatusStrip();
            _visitorListBox = new VisitorListBox();
            _visitorListView = new VisitorListView();

            _visitors.Add(typeof(SplitContainer), _visitorControl);
            _visitors.Add(typeof(SplitterPanel), _visitorControl);
            _visitors.Add(typeof(Panel), _visitorControl);
            _visitors.Add(typeof(GroupBox), _visitorControl);
            _visitors.Add(typeof(ToolStrip), _visitorToolStrip);
            _visitors.Add(typeof(StatusStrip), _visitorStatusStrip);
            _visitors.Add(typeof(ListBox), _visitorListBox);
            _visitors.Add(typeof(ListView), _visitorListView);

            _window = new TipNativeWindow(this);
        }

        /// <summary>
        /// Destroy tool tip window handle.
        /// </summary>
        ~CoolTip()
        {
            DestroyHandle();
        }

        /// <summary>
        /// Top-level control placed on the `BaseForm`.
        /// </summary>
        private Control TopLevelControl
        {
            get
            {
                if (_topControl == null)
                    UpdateTopLevelControl();
                return _topControl;
            }
        }

        /// <summary>
        /// Search and update all sub-controls of the top-level control.
        /// </summary>
        /// <returns>Determined top-level control.</returns>
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

        /// <summary>
        /// Tool tip window handle.
        /// </summary>
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

        /// <summary>
        /// Tool tip window text font.
        /// </summary>
        // Font style, used in the original Delphi component.
        //[DefaultValue(typeof(Font), "Arial, 9pt, style=Bold")]
        [DefaultValue(typeof(Font), "Microsoft Sans Serif, 8.25pt")]
        public Font Font { get; set; }

        /// <summary>
        /// Tool tip window background color.
        /// </summary>
        [DefaultValue(typeof(Color), "Window")]
        public Color BackColor { get; set; }

        /// <summary>
        /// Tool tip window foreground (text) color.
        /// </summary>
        [DefaultValue(typeof(Color), "ControlText")]
        public Color ForeColor { get; set; }

        /// <summary>
        /// Delay between control mouse hover and tool tip first appearance.
        /// </summary>
        [DefaultValue(1000)]
        public int ShowDelay { get; set; }

        /// <summary>
        /// Delay between tool tip appearance and it's disappearance.
        /// I.e. tool tip appearance time.
        /// </summary>
        [DefaultValue(5000)]
        public int HideDelay { get; set; }

        /// <summary>
        /// Delay between switching hovered control without tool tip appearance interruption.
        /// </summary>
        [DefaultValue(100)]
        public int ReshowDelay { get; set; }

        /// <summary>
        /// Padding between tool tip borders and tagret control.
        /// Default: 2px for top and 2px for bottom, 0px for left and right.
        /// </summary>
        [DefaultValue(typeof(Padding), "0, 2, 0, 2")]
        public Padding Marging { get; set; }

        /// <summary>
        /// Padding inside tool tip window between text and border.
        /// Default: 2px for all sides.
        /// </summary>
        [DefaultValue(typeof(Padding), "2, 2, 2, 2")]
        public Padding Padding { get; set; }

        /// <summary>
        /// Icon size inside tool tip window.
        /// Default: 12px * 12px.
        /// </summary>
        [DefaultValue(typeof(Size), "12, 12")]
        public Size IconSize { get; set; }

        /// <summary>
        /// Marging between tool tip icon and window border.
        /// Default: 3px for top and 2px for bottom, 2px for left and right.
        /// </summary>
        [DefaultValue(typeof(Padding), "2, 3, 2, 3")]
        public Padding IconMarging { get; set; }

        /// <summary>
        /// Tool tip window border width.
        /// </summary>
        [DefaultValue(1)]
        public int BorderWidth { get; set; }

        /// <summary>
        /// Tool tip window border color.
        /// </summary>
        [DefaultValue(typeof(Color), "ActiveBorder")]
        public Color BorderColor { get; set; }

        /// <summary>
        /// Component owner form, i.e. top-level control.
        /// </summary>
        [Browsable(true)]
        public Form BaseForm {
            get { return _baseForm; }
            set { _baseForm = value; UpdateTopLevelControl(); }
        }

        /// <summary>
        /// List of all contols visitors.
        /// Make relationship between type and it's visitor.
        /// </summary>
        public Dictionary<Type, IVisitor> Visitors { get { return _visitors; } }

        /// <summary>
        /// List of all assigned tip managers.
        /// Make relationship between type and it's manager.
        /// </summary>
        public Dictionary<Type, IManager> Managers { get { return _managers; } }

        /// <summary>
        /// Dispose object, if needed.
        /// </summary>
        /// <param name="disposing">Is object in disposal state.</param>
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

        /// <summary>
        /// Handler for top-level control creation.
        /// </summary>
        /// <param name="sender">Reference to the top-level control.</param>
        /// <param name="e">Event arguments.</param>
        private void TopLevelCreated(object sender, EventArgs e)
        {
            CreateHandle();
        }

        /// <summary>
        /// Create tool-tip window by wrapper.
        /// </summary>
        private void CreateHandle()
        {
            if (!_isDisposing && IsHandleCreated() == false)
            {
                _window.CreateHandle(GetCreateParams());
            }
        }

        /// <summary>
        /// Handler for top-level control destruction.
        /// </summary>
        /// <param name="sender">Reference to the top-level control.</param>
        /// <param name="e">Event arguments.</param>
        private void TopLevelDestroyed(object sender, EventArgs e)
        {
            DestroyHandle();
        }

        /// <summary>
        /// Destroy tool-tip window with it's wrapper.
        /// </summary>
        private void DestroyHandle()
        {
            if (!_isDisposing && IsHandleCreated())
            {
                _window.DestroyHandle();
            }
        }

        /// <summary>
        /// Handle changing parent event of the currently set top-level control.
        /// Part of the original `ToolTip` code.
        /// </summary>
        /// <param name="s">Reference to the top-level control.</param>
        /// <param name="e">Event arguments</param>
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

        /// <summary>
        /// Reset assigned events of the top-level control.
        /// </summary>
        private void ClearTopLevelControlEvents()
        {
            if (_topControl != null)
            {
                _topControl.ParentChanged -= TopLevelPropertyChanged;
                _topControl.HandleCreated -= TopLevelCreated;
                _topControl.HandleDestroyed -= TopLevelDestroyed;
            }
        }

        /// <summary>
        /// Reset assigned events of the `BaseForm`.
        /// </summary>
        private void ClearBaseFormEvent()
        {
            if (_baseForm != null)
            {
                _baseForm.Deactivate -= ResetHint;
                _baseForm.Move -= ResetHint;
                _baseForm.Resize -= ResetHint;
            }
        }

        /// <summary>
        /// Do reset currently showed tool tip.
        /// </summary>
        /// <param name="sender">Reference to the event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ResetHint(object sender, EventArgs e)
        {
            StopTimerShow();
            DoHide(true);
#if DEBUG
            HidePresentation();
#endif
        }

        /// <summary>
        /// Handler for top-level control handle creation.
        /// </summary>
        /// <param name="sender">Reference to the event sender.</param>
        /// <param name="eventargs">Event arguments</param>
        private void HandleCreated(object sender, EventArgs eventargs)
        {
            // Reset the toplevel control when the owner's handle is recreated.
            ClearTopLevelControlEvents();
            _topControl = null;
            UpdateTopLevelControl();
        }

        /// <summary>
        /// Part of th `IExtenderProvider` which determine possibility to extend target's properties.
        /// </summary>
        /// <param name="target">Control to check extension possibility.</param>
        /// <returns>`True` if extension is possible.</returns>
        public bool CanExtend(object target)
        {
            return !(target is CoolTip)
                && ((target is Control) || (target is ListBox) || (target is ListView));
        }

        /// <summary>
        /// Create and add generic manager with provided functors.
        /// </summary>
        /// <typeparam name="TComponent">Type of the specified component.</typeparam>
        /// <param name="getTip">Functor which returns tip text for specified target.</param>
        /// <param name="getBounds">Functor which returns bounds of the specified target.</param>
        /// <param name="getVisible">Functor which returns visibility of the specified target.</param>
        public void AddManager<TComponent>(
            Func<TComponent, string> getTip,
            Func<TComponent, Rectangle> getBounds,
            Func<TComponent, bool> getVisible)
            where TComponent : Component
        {
            var manager = new Manager<TComponent>();
            manager.Tip = getTip;
            manager.Bounds = getBounds;
            manager.Visible = getVisible;
            _managers.Add(typeof(TComponent), manager);
        }

#if DEBUG
        /// <summary>
        /// Create and add generic visitor with provided functors.
        /// </summary>
        /// <typeparam name="TComponent">Type of the specified component.</typeparam>
        /// <param name="getChildAt">Functor which returns child item / sub-component by given location.</param>
        /// <param name="getChildren">Functor which returns collection of the child items / sub-components.</param>
        public void AddVisitor<TComponent>(
            Func<TComponent, Point, object> getChildAt,
            Func<TComponent, ICollection> getChildren)
            where TComponent : Component
        {
            var visitor = new Visitor<TComponent>();
            visitor.ChildAt = getChildAt;
            visitor.Children = getChildren;
            _visitors.Add(typeof(TComponent), visitor);
        }
#else
        /// <summary>
        /// Create and add generic visitor with provided functors.
        /// </summary>
        /// <typeparam name="TComponent">Type of the specified component.</typeparam>
        /// <param name="getChildAt">Functor which returns child item / sub-component by given location.</param>
        public void AddVisitor<TComponent>(
            Func<TComponent, Point, object> getChildAt)
            where TComponent : Component
        {
            var visitor = new Visitor<TComponent>();
            visitor.ChildAt = getChildAt;
            _visitors.Add(typeof(TComponent), visitor);
        }
#endif


        /// <summary>
        /// Get manager of the specified target component.
        /// Also check derived classes of the available managers.
        /// </summary>
        /// <param name="target">Target component.</param>
        /// <returns>Manager of the specified component.</returns>
        /// <exception cref="IndexOutOfRangeException">Will be thrown if manager for specified component will be not found.</exception>
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

        /// <summary>
        /// Try to recursively find currently hovered target component.
        /// </summary>
        /// <returns>Hovered target component.</returns>
        private object FindCurrentTarget()
        {
            var location = _baseForm.PointToClient(Cursor.Position);
            object target = _baseForm.GetChildAtPoint(location);
            while ((target != null) && _visitors.Keys.Contains(target.GetType()))
            {
                if ((target is ListBox) && !_listBoxes.Contains(target))
                    break;

                if ((target is ListView) && !_listViews.Contains(target))
                    break;

                var visitor = _visitors[target.GetType()];
                object newTarget = visitor.GetItem(target, Cursor.Position);

                if ((newTarget == null) || (target == newTarget))
                    break;

                target = newTarget;
                //Debug.WriteLine("next target: {0}", target);
            }
            return target;
        }

        /// <summary>
        /// Part of the `IMessageFilter`.
        /// Process `WM_MOUSEMOVE` of the `BaseControl`.
        /// Also controls appearance behavior.
        /// </summary>
        /// <param name="msg">Window message.</param>
        /// <returns>Always `False`.</returns>
        public bool PreFilterMessage(ref Message msg)
        {
            var foreground = Native.GetForegroundWindow();
            if (_baseForm == null || _isDisposing || (_baseForm.Handle != foreground))
                return false;

            if (msg.Msg == Native.WM_MOUSEMOVE)
            {
                var target = FindCurrentTarget();
#if DEBUG
                //Debug.WriteLine("{0} {1}", DateTime.Now, msg);
                //Debug.WriteLine("target: {0}", target);
                //Debug.WriteLine("current: {0}", _currentTarget);
                //Debug.WriteLine("future: {0}", _futureTarget);
                //Debug.WriteLine("last: {0}", _lastTarget);
#endif

                if ((target != null)
                    && !target.Equals(_futureTarget)
                    && !target.Equals(_lastTarget)
                    && !target.Equals(_currentTarget)
                    && !target.Equals(_manualTarget))
                {
                    // stop previous show-by-timer
                    StopTimerShow();
                    // start new show-by-timer
                    ShowByTimer(target, ShowDelay);
                }
                if (((target == null) || !target.Equals(_lastTarget))
                    && (DateTime.Now > _lastShowed.AddMilliseconds(ReshowDelay))
                    && (_lastTarget != null))
                {
                    // reset last showed only after delay
                    _lastTarget = null;
                }
                if ((target != null) && (_manualTarget == null))
                {
                    if (!target.Equals(_currentTarget)
                        && (_currentTarget != null))
                    {
                        // show new without additional delay
                        DoShow(target);
                    }
                    else if (!target.Equals(_lastTarget)
                        && (_lastTarget != null))
                    {
                        // reshow new without additional delay
                        DoShow(target);
                    }
                }
            }
            else if (msg.Msg == Native.WM_MOUSELEAVE)
            {
#if DEBUG
                //Debug.WriteLine("{0} {1}", DateTime.Now, msg);
#endif
                var target = FindCurrentTarget();
                if ((_manualTarget == null)
                    && (_currentTarget != target)
                    && (msg.HWnd != Handle)
                    && (foreground != Handle)
                    && !(_renderInfo?.Bounds.Size.IsEmpty ?? false))
                {
                    DoHide(true);
                }
                StopTimerShow();
            }
            else if (msg.Msg == Native.WM_KEYDOWN)
            {
                DoHide(true);
                StopTimerShow();
            }

            return false;
        }

        /// <summary>
        /// Set tip text for specified control, used by form designer.
        /// </summary>
        /// <param name="control">Control to set tool tip text.</param>
        /// <param name="text">Tool tip text.</param>
        public void SetTipText(Control control, string text)
        {
            _targets.Add(control);
            bool added = _managerControl.SetTip(control, text);
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

        /// <summary>
        /// Get tip text for specified control, used by form designer.
        /// </summary>
        /// <param name="control">Control to get tool tip text.</param>
        /// <returns>Tool tip text of the specified control.</returns>
        [DefaultValue("")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string GetTipText(Control control)
        {
            return _managerControl.GetTip(control);
        }

        /// <summary>
        /// Set help text for specified control, used by form designer.
        /// </summary>
        /// <param name="control">Control to set help text.</param>
        /// <param name="help">Help text.</param>
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

        /// <summary>
        /// Get help text for specified control, used by form designer.
        /// </summary>
        /// <param name="control">Control to get help text.</param>
        /// <returns>Help text for the specified control.</returns>
        [DefaultValue("")]
        [Editor(typeof(MultilineStringEditor), typeof(UITypeEditor))]
        public string GetHelpText(Control control)
        {
            return _managerControl.GetHelp(control);
        }

        /// <summary>
        /// Process items of the specified `ListBox`, used by form designer.
        /// </summary>
        /// <param name="listBox">`ListBox` to process.</param>
        /// <returns>`True` if specified `ListBox` added for processing it's items.</returns>
        [DefaultValue(false)]
        public bool GetShowLongItemTips(ListBox listBox)
        {
            return _listBoxes.Contains(listBox);
        }

        /// <summary>
        /// Process items of the specified `ListBox`, used by form designer.
        /// </summary>
        /// <param name="listBox">`ListBox` to process.</param>
        /// <param name="value">Process or not.</param>
        public void SetShowLongItemTips(ListBox listBox, bool value)
        {
            if (value)
                _listBoxes.Add(listBox);
            else
                _listBoxes.Remove(listBox);
        }

        /// <summary>
        /// Process items of the specified `ListView`, used by form designer.
        /// </summary>
        /// <param name="listView">`ListView` to process.</param>
        /// <returns>`True` if specified `ListView` added for processing it's items.</returns>
        [DefaultValue(false)]
        public bool GetShowItemTips(ListView listView)
        {
            return _listViews.Contains(listView);
        }

        /// <summary>
        /// Process items of the specified `ListView`, used by form designer.
        /// </summary>
        /// <param name="listView">`ListView` to process.</param>
        /// <param name="value">Process or not.</param>
        public void SetShowItemTips(ListView listView, bool value)
        {
            if (value)
                _listViews.Add(listView);
            else
                _listViews.Remove(listView);
        }

        /// <summary>
        /// 'Manually' show tip with text and icon for target component.
        /// Resets currently showed tip.
        /// </summary>
        /// <param name="target">Target component to show a tip.</param>
        /// <param name="icon">Icon of the tip.</param>
        /// <param name="delay">Delay of the tip in milliseconds.
        /// Default if `null`. Forever if `0`.</param>
        /// <param name="text">Text of the tip.</param>
        public void Show(object target, Icon icon, int? delay, string text)
        {
            var manager = GetManager(target);
            if (!manager?.GetVisible(target) ?? false)
                return;

            _manualTarget = target;
            var newRenderInfo = new RenderTipInfo(icon, text, delay);
            newRenderInfo.Bounds = GetRenderBounds(newRenderInfo, target, manager);
            bool isNeedToHide = newRenderInfo.IsNeedToHide(_renderInfo);

            DoHide(isNeedToHide);
            DoShow(newRenderInfo, target);

            HideByTimer(delay ?? HideDelay);
        }

        /// <summary>
        /// Show tip with text and exclamation icon for target component
        /// only if the specified expression returns `false`.
        /// Returns expression result.
        /// </summary>
        /// <param name="target">Target component to show a tip.</param>
        /// <param name="expression">Expression to validate.</param>
        /// <param name="text">Text of the tip.</param>
        /// <param name="delay">Delay of the tip in milliseconds.
        /// Default if `null`. Forever if `0`.</param>
        /// <returns>Result of the specified expression.</returns>
        public bool Validate(object target, bool expression, string text, int? delay = null)
        {
            if (expression == false)
            {
                Show(target, Icon.Warning, delay, text);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Hide currently showing tip.
        /// </summary>
        public void Hide()
        {
            if (_manualTarget != null)
                DoHide(true);
        }

        /// <summary>
        /// Handler process `BaseForm` help request (used by `?` button on the form caption.)
        /// </summary>
        /// <param name="sender">Reference for the sender.</param>
        /// <param name="e">Event arguments.</param>
        private void HelpRequested(object sender, HelpEventArgs e)
        {
            _manualTarget = sender;
            string text = _managerControl.GetHelp(sender);
            var newRenderInfo = new RenderTipInfo(Icon.Information, text, 0);
            newRenderInfo.Bounds = GetRenderBounds(newRenderInfo, sender, _managerControl);
            bool isNeedToHide = newRenderInfo.IsNeedToHide(_renderInfo);

            DoHide(isNeedToHide);
            DoShow(newRenderInfo, sender);

            e.Handled = true;
        }

        /// <summary>
        /// Determines is tool tip window handle created or not.
        /// </summary>
        /// <returns>`True` if handle is already created.</returns>
        internal bool IsHandleCreated()
        {
            return (_window != null) && (_window.Handle != IntPtr.Zero);
        }

        /// <summary>
        /// Set tool tip window style and parent.
        /// </summary>
        /// <returns>Filled `CreateParams` object with needed parameters.</returns>
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

        /// <summary>
        /// Do hide current tip if any.
        /// </summary>
        private void DoHide(bool hideWindow)
        {
            if (_currentTarget != null && !_isDisposing)
            {
                if (hideWindow)
                {
                    var handle = new HandleRef(this, Handle);
                    Native.ShowWindow(handle, Native.SW_HIDE);
                }

                StopTimerHide();
                if (_manualTarget == null)
                    _lastTarget = _currentTarget;
                _currentTarget = null;
                _renderInfo = null;
                _manualTarget = null;
                _lastShowed = DateTime.Now;
            }
        }

        /// <summary>
        /// Start timer for tip delayed hide.
        /// </summary>
        /// <param name="delay">Delay in milliseconds.</param>
        private void HideByTimer(int delay)
        {
            if ((_timerHide == null) && (delay > 0))
            {
                _timerHide = new TipTimer(null);
                _timerHide.Tick += TimerHideHandler;
                _timerHide.Interval = delay;
                _timerHide.Start();
            }
        }

        /// <summary>
        ///  Cancel delayed tip hide.
        /// </summary>
        protected void StopTimerHide()
        {
            if (_timerHide != null)
            {
                _timerHide.Stop();
                _timerHide.Dispose();
                _timerHide = null;
            }
        }

        /// <summary>
        /// Timer handler for tip delayed hide.
        /// </summary>
        /// <param name="source">Timer reference.</param>
        /// <param name="args">Event arguments.</param>
        private void TimerHideHandler(object source, EventArgs args)
        {
            DoHide(true);
        }

        /// <summary>
        /// Start timer for tip delayed show.
        /// </summary>
        /// <param name="target">Target component of the future tip.</param>
        /// <param name="interval">Delay in milliseconds.</param>
        private void ShowByTimer(object target, int interval)
        {
            if (_timerShow == null)
            {
                GetManager(target)?.GetShowInterval(target, ref interval);
                if (interval == 0)
                {
                    DoShow(target);
                }
                else
                {
                    _futureTarget = target;
                    _timerShow = new TipTimer(target);
                    _timerShow.Tick += TimerShowHandler;
                    _timerShow.Interval = interval;
                    _timerShow.Start();
                }
#if DEBUG
                //Debug.WriteLine("{0}\tshow timer start for {1}", DateTime.Now, _futureTarget);
#endif
            }
        }

        /// <summary>
        /// Cancel delayed tip show.
        /// </summary>
        protected void StopTimerShow()
        {
            if (_timerShow != null)
            {
#if DEBUG
                //Debug.WriteLine("{0}\tshow timer stop for {1}", DateTime.Now, _futureTarget);
#endif
                _timerShow.Stop();
                _timerShow.Dispose();
                _timerShow = null;
                _futureTarget = null;
            }
        }

        /// <summary>
        /// Timer handler for tip delayed show.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void TimerShowHandler(object source, EventArgs args)
        {
            var timer = source as TipTimer;
            if ((_manualTarget == null) && _baseForm.Bounds.Contains(Cursor.Position))
                DoShow(timer.Target);
        }

        /// <summary>
        /// Measure text bounds with current font.
        /// </summary>
        /// <param name="text">Text to measure bound of.</param>
        /// <returns></returns>
        private Size GetTextRect(string text)
        {
            return TextRenderer.MeasureText(text, Font);
        }

        /// <summary>
        /// Determine tip manager and process real tip show for specified target.
        /// </summary>
        /// <param name="target">Target for the tip.</param>
        private void DoShow(object target)
        {
            var manager = GetManager(target);
            if (!manager?.GetVisible(target) ?? false)
                return;

            string hint = manager.GetTip(target);
            if (String.IsNullOrWhiteSpace(hint))
                return;

            var newRenderInfo = new RenderTipInfo(hint);
            newRenderInfo.Bounds = GetRenderBounds(newRenderInfo, target, manager);
            bool isNeedToHide = newRenderInfo.IsNeedToHide(_renderInfo);

            DoHide(isNeedToHide);
            DoShow(newRenderInfo, target);

            HideByTimer(HideDelay);
        }

        /// <summary>
        /// Calculate bounds of the tool tip window.
        /// General geometry.
        /// </summary>
        /// <param name="renderInfo">Render information.</param>
        /// <param name="target">Tool tip window target object.</param>
        /// <param name="manager">Tip manager for target object.</param>
        /// <returns>Bounds of the tool tip window.</returns>
        private Rectangle GetRenderBounds(RenderTipInfo renderInfo, object target, IManager manager)
        {
            // calculate general variables, like size, location, etc.
            var bounds = manager.GetBounds(target);
            var location = new Point(bounds.Left - Marging.Left, bounds.Bottom + Marging.Bottom);
            var border = new Padding(BorderWidth);
            var size = GetTextRect(renderInfo.Info.Text) + Padding.Size + border.Size;
            var screen = Screen.FromRectangle(bounds).Bounds;
            var window = _baseForm.Bounds;

            // extend bounds for icon
            size.Width += IconSize.Width + IconMarging.Horizontal;
            var rect = new Rectangle(location, size);

            // try to place tip inside base form: check bottom
            bool isOutOfBottom = (rect.Bottom > window.Bottom)
                && (rect.Height < window.Height);
            if (isOutOfBottom)
            {
                rect.Y = bounds.Top - size.Height - Marging.Top - border.Horizontal;
                renderInfo.MoveIconDown();
            }

            // try to place tip inside base form: check right
            bool isOutOfRight = (rect.Right > window.Right);
            int futureLeft = bounds.Left - size.Width - Marging.Left - border.Horizontal;
            bool isOutOfLeft = (futureLeft < window.Left);
            bool widtherThanWindow = (rect.Width < window.Width);
            bool isOutOfScreen = (rect.Right > screen.Width);

            // reposition icon to the right if tip already crossed right bound
            if ((isOutOfRight && !isOutOfLeft && widtherThanWindow) || isOutOfScreen)
            {
                rect.X = futureLeft;
                renderInfo.MoveIconRight();
            }
            if (rect.Width * 2 < bounds.Width)
            {
                renderInfo.RedirectArrowRight();
            }
            return rect;
        }

        /// <summary>
        /// Internal routine to show tool tip window.
        /// </summary>
        /// <param name="newRenderInfo">New render information.</param>
        /// <param name="newTarget">New tool tip window target object.</param>
        private void DoShow(RenderTipInfo newRenderInfo, object newTarget)
        {
            // override previous render info
            _renderInfo = newRenderInfo;
            _currentTarget = newTarget;
            // reposition and show tip window
            var bounds = _renderInfo.Bounds;
            var handle = new HandleRef(this, Handle);
            Native.SetWindowPos(handle, Native.HWND_TOPMOST,
                bounds.Left, bounds.Top, bounds.Width, bounds.Height,
                Native.SWP_NOACTIVATE);
            Native.ShowWindow(handle, Native.SW_SHOWNOACTIVATE);
            Native.RedrawWindow(handle, IntPtr.Zero, IntPtr.Zero, Native.RDW_INVALIDATE);
            StopTimerShow();
        }

        /// <summary>
        /// Draw elements of the tool tip window.
        /// </summary>
        /// <param name="graphics">Graphics of window itself.</param>
        /// <param name="bounds">Window bounds.</param>
        /// <param name="info">Tool tip info (i.e. icon, text, etc.).</param>
        private void Draw(Graphics graphics, Rectangle bounds, RenderTipInfo info)
        {
            bounds.Width -= 1;
            bounds.Height -= 1;

            // draw base elements (background, border)
            using (var brush = new SolidBrush(BackColor))
                graphics.FillRectangle(brush, bounds);
            using (var pen = new Pen(BorderColor, BorderWidth))
                graphics.DrawRectangle(pen, bounds);
            using (var brush = new SolidBrush(info.IconBackground))
            {
                var border = new Padding(BorderWidth);
                var text = GetTextRect(info.Info.Text);

                // determine icon position and draw it's background:
                // right side or left side of the window
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

                // determine positions and draw icon itself:
                // at the bottom or at the top of the window
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

            // determine location-and-size and draw tool tip text
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

        /// <summary>
        /// Window messages filter process for tool tip window.
        /// </summary>
        /// <param name="msg">Window message to process.</param>
        private void WndProc(ref Message msg)
        {
            switch (msg.Msg)
            {
                case Native.WM_PRINTCLIENT:
                case Native.WM_PAINT:
                    {
                        Native.PAINTSTRUCT lpPaint = default;
                        IntPtr hdc = Native.BeginPaint(new HandleRef(this, msg.HWnd), ref lpPaint);
                        try
                        {
                            Rectangle bounds = new Rectangle(
                                lpPaint.rcPaint_left, lpPaint.rcPaint_top,
                                lpPaint.rcPaint_right - lpPaint.rcPaint_left,
                                lpPaint.rcPaint_bottom - lpPaint.rcPaint_top);
                            if (bounds == Rectangle.Empty)
                                break;

                            // double buffered rendering
                            using (var graphics = Graphics.FromHdc(hdc))
                            {
                                using (var buffer = BufferedGraphicsManager.Current.Allocate(graphics, bounds))
                                {
                                    Draw(buffer.Graphics, bounds, _renderInfo);
                                    buffer.Render(graphics);
                                }
                            }
                        }
                        finally
                        {
                            Native.EndPaint(new HandleRef(this, msg.HWnd), ref lpPaint);
                        }
                    }
                    break;
            }

            // default window message processing
            if (_window != null)
            {
                _window.DefWndProc(ref msg);
            }
        }

#if DEBUG
        /// <summary>
        /// Storage of all created tool tip windows.
        /// </summary>
        private HashSet<TipNativeWindow> _windows;

        /// <summary>
        /// Determine is presentation currently running.
        /// </summary>
        public bool IsPresentationRunning { get { return _windows != null; } }

        /// <summary>
        /// Create and show tool tip for all accessible
        /// controls / components of the <seealso cref="BaseForm"/>.
        /// </summary>
        public void ShowPresentation()
        {
            _windows = new HashSet<TipNativeWindow>();
            DoPresentation(_baseForm.Controls);
        }

        /// <summary>
        /// Hide and destroy all tool tip window.
        /// </summary>
        public void HidePresentation()
        {
            if (_windows != null)
            {
                foreach (var window in _windows)
                    window.DestroyHandle();
                _windows = null;
            }
        }

        /// <summary>
        /// Recursively visit all given collection of the
        /// controls / components and Create and show tool tip for all
        /// accessible targets.
        /// </summary>
        /// <param name="collection">Collection of the controls / components.</param>
        private void DoPresentation(ICollection collection)
        {
            foreach (object target in collection)
            {
                if (_visitors.Keys.Contains(target.GetType()))
                {
                    if ((target is ListBox) && !_listBoxes.Contains(target))
                        break;

                    if ((target is ListView) && !_listViews.Contains(target))
                        break;

                    var visitor = _visitors[target.GetType()];
                    var items = visitor.GetItems(target);
                    if (items != null)
                        DoPresentation(items);
                }
                else
                {
                    DoShow(target);
                    _windows.Add(_window);
                    _window = new TipNativeWindow(this);
                }
            }
        }
#endif
    }
}
