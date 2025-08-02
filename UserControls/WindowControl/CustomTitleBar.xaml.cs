using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using ChangeFolderIcon.Utils.WindowsAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using WinRT.Interop;

namespace ChangeFolderIcon.UserControls.WindowControl
{
    public sealed partial class CustomTitleBar : UserControl
    {
        private Window? _parentWindow;
        private AppWindow? _appWindow;
        private bool _firstLayoutHandled = false;

        // �����¼����Ա� MainWindow ���Լ���
        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs>? TextChanged;
        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs>? SuggestionChosen;
        public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs>? QuerySubmitted;

        public event EventHandler? SettingsClicked;

        // ���� ItemsSource ����
        public object ItemsSource
        {
            get => TitleBarSearchBox.ItemsSource;
            set => TitleBarSearchBox.ItemsSource = value;
        }

        public CustomTitleBar()
        {
            this.InitializeComponent();
            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;
            this.LayoutUpdated += OnLayoutUpdated;

            // ���ڲ��ؼ����¼����ӵ������¼�
            TitleBarSearchBox.TextChanged += (s, a) => TextChanged?.Invoke(s, a);
            TitleBarSearchBox.SuggestionChosen += (s, a) => SuggestionChosen?.Invoke(s, a);
            TitleBarSearchBox.QuerySubmitted += (s, a) => QuerySubmitted?.Invoke(s, a);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // ���Ի�ȡ������
            _parentWindow = GetParentWindow();
            if (_parentWindow == null) return;

            // ��ȡAppWindow
            var hWnd = WindowNative.GetWindowHandle(_parentWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow != null)
            {
                // ��������չ������������
                _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                _appWindow.SetIcon("Assets\\icon\\app_icon.ico");

                // ���Ĵ��ںͿؼ���С�仯�¼����Ա�����Ҫʱ�����϶�����
                _parentWindow.SizeChanged += OnParentWindowSizeChanged;
                this.SizeChanged += OnParentWindowSizeChanged;

                // �ӳ�һ֡��ȷ���������
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, UpdateDragRegions);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // �����¼����ģ���ֹ�ڴ�й©
            if (_parentWindow != null)
            {
                _parentWindow.SizeChanged -= OnParentWindowSizeChanged;
            }
            this.SizeChanged -= OnParentWindowSizeChanged;
        }

        #region UI �¼� ���� ��֡������ɺ��ټ�����ק��
        /// <summary>
        /// ��֡������� Measure/Arrange ʱ��������ִ��һ�Ρ�
        /// </summary>
        private void OnLayoutUpdated(object? sender, object e)
        {
            if (_firstLayoutHandled || _appWindow is null) return;

            if (ActualWidth > 0 && ActualHeight > 0 && TitleColumn.ActualWidth > 0)
            {
                UpdateDragRegions();
                _firstLayoutHandled = true;
            }
        }

        /// <summary>
        /// �������ڻ�ؼ��ߴ�仯ʱ�����϶�����
        /// </summary>
        private void OnParentWindowSizeChanged(object sender, object e)
        {
            if (_appWindow?.TitleBar.ExtendsContentIntoTitleBar == true)
                UpdateDragRegions();
        }
        #endregion

        #region �϶��������
        /// <summary>
        /// ���㲢���±������Ŀ��϶�����
        /// </summary>
        private void UpdateDragRegions()
        {
            if (_appWindow == null || _parentWindow == null) return;
            if (this.ActualWidth == 0 || this.ActualHeight == 0) return;

            // 1. ��ȡDPI���ű���
            double scale = DpiHelper.GetScaleAdjustment(_parentWindow);

            // 2. ����Ϊϵͳ��ť�����Ŀհ�������
            LeftPaddingColumn.Width = new GridLength(_appWindow.TitleBar.LeftInset / scale);
            RightPaddingColumn.Width = new GridLength(_appWindow.TitleBar.RightInset / scale);

            // 3. �����еĿ�Ⱦ�ȷ������϶�����
            var dragRects = new List<RectInt32>();

            // �����һ���϶����� (ͼ�� + ���� + ���հ�����)
            // X �����ϵͳԤ����+ͷ��֮��ʼ
            double rect1X = LeftPaddingColumn.ActualWidth + HeaderColumn.ActualWidth;
            // ���������������϶��еĿ��֮��
            var rect1Width = IconColumn.ActualWidth + TitleColumn.ActualWidth + LeftDragColumn.ActualWidth;

            if (rect1Width > 0)
            {
                dragRects.Add(new RectInt32(
                    (int)(rect1X * scale),
                    0,
                    (int)(rect1Width * scale),
                    (int)(AppTitleBar.ActualHeight * scale)
                ));
            }

            // ����ڶ����϶����� (�������Ҳ�Ŀհ�����)
            // X ��������������У��������϶����򣩵Ŀ��֮��
            var rect2X = rect1X + rect1Width + ContentColumn.ActualWidth;
            // ������Ҳ��϶��еĿ��
            var rect2Width = RightDragColumn.ActualWidth;

            if (rect2Width > 0)
            {
                dragRects.Add(new RectInt32(
                    (int)(rect2X * scale),
                    0,
                    (int)(rect2Width * scale),
                    (int)(AppTitleBar.ActualHeight * scale)
                ));
            }

            // 4. �������յĿ��϶�����
            _appWindow.TitleBar.SetDragRectangles(dragRects.ToArray());
        }
        #endregion

        #region ����
        /// <summary>
        /// ���ϱ�����������ȡ���� Window
        /// </summary>
        /// <returns></returns>
        private Window? GetParentWindow()
        {
            // ���ϱ������ӻ����ҵ�Window
            var parent = VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return App.window;
        }
        #endregion

        #region ���ð�ť���
        /// <summary>
        /// �������ð�ť�ĵ���¼���
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // �����¼�
            SettingsClicked?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}
