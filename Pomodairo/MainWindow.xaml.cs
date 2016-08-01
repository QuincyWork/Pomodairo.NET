using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Media;
using System.Windows.Resources;
using System.IO;
using System.Windows.Media.Animation;

namespace Pomodairo
{
    
    enum TaskStatus
    {        
        TaskStop,
        TaskRunning,
        TaskRest
    };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double TastListGridHeight = 0;
        private double TastControlGridHeight = 0;
        private double TaskTimerGridHeight = 0;
        private DispatcherTimer TaskTimer = null;
        private TimeSpan TaskLeaveTime = new TimeSpan(0, 25, 0);
        private int CurrentSelectIndex = -1;
        private TaskStatus CurrentTaskStatus = TaskStatus.TaskStop;
        private ObservableCollection<TaskItem> TaskListData = null;
        private int CurrentRestCount = 0;
        private int CurrentRestTime = 0;
        private Storyboard TaskTimerLableSB = new Storyboard();
        private DateTime CurrentTaskBeginTime;

        public MainWindow()
        {
            InitializeComponent();
            TaskTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1.0),
                DispatcherPriority.Loaded,
                new EventHandler(this.doTaskTimerTick),
                Dispatcher);
            TaskTimer.Stop();

            TaskListData = new ObservableCollection<TaskItem>();

            // 添加动画
            DoubleAnimation animation = new DoubleAnimation();
            animation.To = 0.0;
            animation.Duration = TimeSpan.FromSeconds(0.2);
            TaskTimerLableSB.Children.Add(animation);
            TaskTimerLableSB.AutoReverse = true;
            TaskTimerLableSB.RepeatBehavior = new RepeatBehavior(10);

            DependencyProperty[] propertyChain = new DependencyProperty[]
            {
                 TextElement.ForegroundProperty,
                 SolidColorBrush.OpacityProperty
            };

            Storyboard.SetTarget(animation, LabelTimer);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(0).(1)", propertyChain));
            TaskTimerLableSB.Completed += new EventHandler((object sender1, EventArgs e1) => {

                if (CurrentTaskStatus != TaskStatus.TaskStop)
                {
                    TaskTimer.Start();
                }
            });
        }

        #region 控件响应事件
        private void btnTaskList_Click(object sender, RoutedEventArgs e)
        {
            if (TaskListGrid.Visibility == Visibility.Visible)
            {
                TaskListGrid.Visibility = Visibility.Collapsed;
                TastListGridHeight = MainGrid.RowDefinitions[3].Height.Value;
                MainGrid.RowDefinitions[3].Height = new GridLength(0);
            }
            else
            {
                MainGrid.RowDefinitions[3].Height = new GridLength(TastListGridHeight);
                TaskListGrid.Visibility = Visibility.Visible;
            }
        }

        private void btnStartTask_Click(object sender, RoutedEventArgs e)
        {
            TaskLeaveTime = new TimeSpan(0, 25, 0);
            LabelTimer.Foreground = new SolidColorBrush(Colors.Yellow);
            LabelTimer.Content = "25:00";

            // 任务停止
            if (TaskTimer.IsEnabled)
            {
                TaskTimer.Stop();
                btnStartTask.Content = "开始";                
                
                if ((CurrentTaskStatus == TaskStatus.TaskRunning) &&
                    (CurrentSelectIndex != -1))
                {
                    if (DateTime.Now > CurrentTaskBeginTime.AddMinutes(5))
                    {
                        // Update Item to Calender
                        OutlookTaskManager.UpdateTaskCalendar(
                            TaskListData[CurrentSelectIndex],
                            CurrentTaskBeginTime,
                            DateTime.Now);
                    }
                }

                btnSynch.IsEnabled = true;
                CurrentTaskStatus = TaskStatus.TaskStop;
            }
            else if (CurrentSelectIndex != -1)
            {
                TaskTimer.Start();
                btnStartTask.Content = "结束";
                CurrentTaskStatus = TaskStatus.TaskRunning;
                btnSynch.IsEnabled = false;
                TaskTimerLableSB.Stop();
                CurrentTaskBeginTime = DateTime.Now;

                SoundPlayer player = new SoundPlayer(Pomodairo.Properties.Resources.ticking);
                player.Play();
            }
        }

        private void btnMin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (TaskControlBar.Visibility == Visibility.Visible)
            {
                if (TaskListGrid.Visibility == Visibility.Visible)
                {
                    TaskListGrid.Visibility = Visibility.Collapsed;
                    TastListGridHeight = MainGrid.RowDefinitions[3].Height.Value;
                    MainGrid.RowDefinitions[3].Height = new GridLength(0);
                }

                TaskControlBar.Visibility = Visibility.Collapsed;
                TastControlGridHeight = MainGrid.RowDefinitions[2].Height.Value;
                MainGrid.RowDefinitions[2].Height = new GridLength(0);

                // 隐藏开始按钮
                btnStartTask.Visibility = Visibility.Collapsed;
                btnNext.Visibility = Visibility.Collapsed;

                TaskTimerGridHeight = MainGrid.RowDefinitions[1].Height.Value;
                MainGrid.RowDefinitions[1].Height = new GridLength(TaskTimerGridHeight / 4);

                // 设置字体大小
                LabelTimer.FontSize = 24;

                // 设置位置
                LabelTimer.HorizontalAlignment = HorizontalAlignment.Right;
                LabelTask.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                MainGrid.RowDefinitions[2].Height = new GridLength(TastControlGridHeight);
                TaskControlBar.Visibility = Visibility.Visible;

                // 显示开始按钮
                MainGrid.RowDefinitions[1].Height = new GridLength(TaskTimerGridHeight);
                btnStartTask.Visibility = Visibility.Visible;
                btnNext.Visibility = Visibility.Visible;

                LabelTimer.FontSize = 80;
                LabelTimer.HorizontalAlignment = HorizontalAlignment.Left;
                LabelTask.VerticalAlignment = VerticalAlignment.Bottom;
            }
        }

        private void btnSynch_Click(object sender, RoutedEventArgs e)
        {
            string currentYear = System.DateTime.Now.ToString("yyyy日志");
            string currentDay = System.DateTime.Now.ToString("d日");
            string crrrentMonth = DateTime.Now.ToString("MMMM", new System.Globalization.CultureInfo("en-us"));
            List<TaskItem> taskList = OneNoteTaskManager.ReadTaskList(currentYear, crrrentMonth, currentDay);
            if (taskList.Count > 0)
            {
                TaskListData.Clear();
                foreach (var item in taskList)
                {
                    TaskListData.Add(item);
                }

                TaskGrid.ItemsSource = TaskListData;
            }
        }

        #endregion

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point position = e.GetPosition(this.MainGrid);
            if ((position.X >= this.MainGrid.ActualWidth ? false : position.Y < this.MainGrid.ActualHeight))
            {
                DragMove();
            }
        }

        private void doTaskTimerTick(object sender, EventArgs e)
        {
            switch (CurrentTaskStatus)
            {
                case TaskStatus.TaskRunning:
                    {
                        if (TaskLeaveTime.TotalSeconds > 0)
                        {
                            if (TaskLeaveTime.TotalSeconds == 60)
                            {
                                LabelTimer.Foreground = new SolidColorBrush(Colors.Red);
                            }

                            TaskLeaveTime = TaskLeaveTime.Subtract(new TimeSpan(0, 0, 1));
                        }
                        else
                        {
                            CurrentTaskStatus = TaskStatus.TaskRest;
                            LabelTimer.Foreground = new SolidColorBrush(Color.FromRgb(0,255,0));
                            CurrentRestCount++;
                            CurrentRestTime = CurrentRestCount % 4 == 0 ? 20 : 5;
                            CurrentRestTime *= 60;

                            TaskListData[CurrentSelectIndex].TaskTimeUsage++;

                            // Update OneNote data
                            OneNoteTaskManager.UpdateTaskItem(TaskListData[CurrentSelectIndex]);
                            
                            TaskGrid.ItemsSource = null;
                            TaskGrid.ItemsSource = TaskListData;

                            LabelTask.Content = String.Format("{0}({1}/{2})",
                                TaskListData[CurrentSelectIndex].TaskName,
                                TaskListData[CurrentSelectIndex].TaskTimeUsage,
                                TaskListData[CurrentSelectIndex].TaskTimeEdit);

                            SoundPlayer player = new SoundPlayer(Pomodairo.Properties.Resources.alarm);
                            player.Play();

                            TaskTimerLableSB.RepeatBehavior = new RepeatBehavior(20);                            
                            TaskTimerLableSB.Begin();
                            TaskTimer.Stop();

                            // Update Item to Calender
                            OutlookTaskManager.UpdateTaskCalendar(
                                TaskListData[CurrentSelectIndex],
                                DateTime.Now.AddMinutes(-25),
                                DateTime.Now);

                            // 闪烁窗口
                            //WindowExtensions.FlashWindow(Application.Current.MainWindow, 10);

                            if (this.WindowState == WindowState.Minimized)
                            {
                                this.WindowState = WindowState.Normal;
                            }
                        }
                    }
                    break;

                case TaskStatus.TaskRest:
                    {
                        if (TaskLeaveTime.TotalSeconds < CurrentRestTime)
                        {
                            TaskLeaveTime = TaskLeaveTime.Add(new TimeSpan(0, 0, 1));
                        }
                        else
                        {
                            CurrentTaskStatus = TaskStatus.TaskStop;

                            // 动画效果
                            TaskTimerLableSB.RepeatBehavior = new RepeatBehavior(50);
                            TaskTimerLableSB.Begin();
                        }
                    }
                    break;

                case TaskStatus.TaskStop:
                    {
                        TaskTimer.Stop();
                        btnStartTask.Content = "开始";
                        btnNext.Visibility = Visibility.Visible;
                        btnSynch.IsEnabled = true;
                    }
                    break;
            }

            LabelTimer.Content = String.Format("{0:D2}:{1:D2}", TaskLeaveTime.Minutes, TaskLeaveTime.Seconds);
        }

        private void TaskGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!TaskTimer.IsEnabled)
            {
                CurrentSelectIndex = TaskGrid.SelectedIndex;
                if (CurrentSelectIndex != -1)
                {
                    LabelTask.Content = String.Format("{0}({1}/{2})",
                                TaskListData[CurrentSelectIndex].TaskName,
                                TaskListData[CurrentSelectIndex].TaskTimeUsage,
                                TaskListData[CurrentSelectIndex].TaskTimeEdit);

                    TaskTimerLableSB.Stop();
                }
            }
        }

        private void btnInterruption_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectIndex != -1)
            {
                TaskListData[CurrentSelectIndex].TaskTimeInterrupt++;
                TaskGrid.ItemsSource = null;
                TaskGrid.ItemsSource = TaskListData;

                OneNoteTaskManager.UpdateTaskItem(TaskListData[CurrentSelectIndex]);
            }
        }

        private void btnUnplan_Click(object sender, RoutedEventArgs e)
        {
            // Aero效果
            //ExtendAeroGlass.AddExtendAeroGlass(Application.Current.MainWindow);
            //OutlookTaskManager.ListTaskCalendar();
            if (CurrentSelectIndex != -1)
            {
               // OutlookTaskManager.UpdateTaskCalendar(TaskListData[CurrentSelectIndex]);
            }
        }
    }
}
