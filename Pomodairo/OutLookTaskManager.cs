using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Outlook;
using System.Windows;

namespace Pomodairo
{
    class OutlookTaskManager
    {
        public static void UpdateTaskCalendar(TaskItem item, DateTime dtStart, DateTime dtEnd)
        {
            try
            {
                var outlookApp = new Microsoft.Office.Interop.Outlook.Application();
                var mapiNamespace = outlookApp.GetNamespace("MAPI");
                var calendarFolder = mapiNamespace.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderCalendar);
                var outlookCalendarItems = calendarFolder.Items;

                // find exists calender item.
                AppointmentItem oAppointment = null;
                string strBody = string.Format("OneNoteTaskID:\t{0}\r\nOneNoteURL:\t{1}\\{2}\\{3}"
                    , item.TaskOneNoteId
                    , item.NoteBookName
                    , item.SectionName
                    , item.PageName);

                //string filter = "[Start] >= '2016/6/19' and [Subject]='"+ item.TaskName + "'";
                // 查找最近30分钟之内的上次同一个任务
                string filter = "[Subject]='" + item.TaskName + "'" + " and " +
                    "[End] >= '" + dtStart.AddMinutes(-30).ToString("yyyy/M/d H:mm") + "'";

                oAppointment = outlookCalendarItems.Find(filter);
                if (oAppointment != null)
                {
                    if (!oAppointment.Body.Contains(string.Format("OneNoteTaskID:\t{0}", item.TaskOneNoteId)))
                    {
                        oAppointment = null;
                    }

                    //if (dtStart > oAppointment.End.AddMinutes(30))
                    //{
                    //    oAppointment = null;
                    //}
                }

                if (oAppointment == null)
                {
                    oAppointment = (AppointmentItem)outlookApp.CreateItem(OlItemType.olAppointmentItem);
                    oAppointment.Start = dtStart;
                    oAppointment.Body = strBody;
                    oAppointment.Subject = item.TaskName;
                }

                oAppointment.End = dtEnd;
                oAppointment.ReminderSet = false;
                oAppointment.ReminderPlaySound = false;
                oAppointment.Importance = OlImportance.olImportanceNormal;
                oAppointment.BusyStatus = OlBusyStatus.olBusy;
                oAppointment.Categories = item.TaskClass.Name;

                //This method save the appointment to the outlook
                oAppointment.Save();
            }
            catch (System.Exception e)
            {
     
            }
        }

        public static void ListTaskCalendar()
        {
            var outlookApp = new Microsoft.Office.Interop.Outlook.Application();
            var mapiNamespace = outlookApp.GetNamespace("MAPI");
            var CalendarFolder = mapiNamespace.GetDefaultFolder(Microsoft.Office.Interop.Outlook.OlDefaultFolders.olFolderCalendar);

            var outlookCalendarItems = CalendarFolder.Items;
            outlookCalendarItems.IncludeRecurrences = true;

            foreach (Microsoft.Office.Interop.Outlook.AppointmentItem item in outlookCalendarItems)
            {
                if (item.IsRecurring)
                {
                    Microsoft.Office.Interop.Outlook.RecurrencePattern rp = item.GetRecurrencePattern();
                    DateTime first = new DateTime(2008, 8, 31, item.Start.Hour, item.Start.Minute, 0);
                    DateTime last = new DateTime(2008, 10, 1);
                    Microsoft.Office.Interop.Outlook.AppointmentItem recur = null;
                    for (DateTime cur = first; cur <= last; cur = cur.AddDays(1))
                    {
                        try
                        {
                            recur = rp.GetOccurrence(cur);
                            MessageBox.Show(recur.Subject + " -> " + cur.ToLongDateString());
                        }
                        catch
                        { }
                    }
                }
                else
                {
                    MessageBox.Show(item.Subject + " -> " + item.Start.ToLongDateString());
                }
            }


        }
    }
}
