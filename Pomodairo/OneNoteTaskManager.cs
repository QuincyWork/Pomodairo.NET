using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.OneNote;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Pomodairo
{
    class OneNoteTaskManager
    {
        public static string NoHtml(string htmlstring)
        {
            //删除HTML   
            htmlstring = Regex.Replace(htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"([/r/n])[/s]+", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(nbsp|#160);", "   ", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(iexcl|#161);", "/xa1", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(cent|#162);", "/xa2", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(pound|#163);", "/xa3", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&(copy|#169);", "/xa9", RegexOptions.IgnoreCase);
            htmlstring = Regex.Replace(htmlstring, @"&#(/d+);", "", RegexOptions.IgnoreCase);
            htmlstring.Replace("<", "");
            htmlstring.Replace(">", "");
            htmlstring.Replace("/r/n", "");

            return htmlstring;
        }

        public static List<TaskItem> ReadTaskList(string notebook, string section, string page)
        {
            List<TaskItem> taskList = new List<TaskItem>();

            string notebookXml;
            var onenoteApp = new Microsoft.Office.Interop.OneNote.Application();
            onenoteApp.GetHierarchy(null, HierarchyScope.hsPages, out notebookXml);

            var doc = System.Xml.Linq.XDocument.Parse(notebookXml);
            var ns = doc.Root.Name.Namespace;

            var notebookNodeList = doc.Descendants(ns + "Notebook").Where(n =>
                    n.Attribute("name").Value.Contains(notebook));
            if (notebookNodeList == null)
            {
                return taskList;
            }

            foreach (var notebookNode in notebookNodeList)
            {
                var sectionNode = notebookNode.Descendants(ns + "Section").Where(n =>
                        n.Attribute("name").Value.Contains(section)).FirstOrDefault();
                if (sectionNode != null)
                {
                    var pageNode = sectionNode.Descendants(ns + "Page")
                                  .Where(n => n.Attribute("name").Value.Contains(page))
                                  .FirstOrDefault();
                    if (pageNode != null)
                    {
                        string pageXml;
                        onenoteApp.GetPageContent(pageNode.Attribute("ID").Value, out pageXml);
                        var pageDoc = System.Xml.Linq.XDocument.Parse(pageXml);

                        Dictionary<int, TaskItemClass> TagDefsDic = new Dictionary<int, TaskItemClass>();
                        var tagDefs = pageDoc.Descendants(ns + "TagDef");
                        foreach (var tagDef in tagDefs)
                        {
                            TaskItemClass ti = new TaskItemClass();
                            int value = 0;
                            int.TryParse(tagDef.Attribute("index").Value, out value);
                            ti.Index = value;
                            ti.Name = tagDef.Attribute("name").Value;
                            ti.Color = tagDef.Attribute("fontColor").Value;
                            TagDefsDic.Add(ti.Index, ti);
                        }

                        var taskTable = pageDoc.Descendants(ns + "Table").FirstOrDefault();
                        if (taskTable != null)
                        {
                            bool bfirst = true;
                            foreach (var taskRow in from row in taskTable.Descendants(ns + "Row") select row)
                            {
                                if (bfirst)
                                {
                                    bfirst = false;
                                    continue;
                                }

                                string[] cellValues = new string[5];
                                int index = 0;
                                string strTaskId = "";
                                bool bTaskComplete = false;
                                int tagDefIndex = -1;

                                foreach (var taskCell in from cells in taskRow.Descendants(ns + "Cell") select cells)
                                {
                                    if (index == 0)
                                    {
                                        var taskId = taskCell.Descendants(ns + "OutlookTask").FirstOrDefault();
                                        if (taskId == null)
                                        {
                                            break;
                                        }
                                        strTaskId = taskId.Attribute("guidTask").Value;
                                        bTaskComplete = (taskId.Attribute("completed").Value == "true");
                                        if(string.IsNullOrEmpty(strTaskId))
                                        {
                                            break;
                                        }

                                        // Get TagDef
                                        var taskTag = taskCell.Descendants(ns + "Tag").FirstOrDefault();
                                        if (taskTag != null)
                                        {
                                            int.TryParse(taskTag.Attribute("index").Value, out tagDefIndex);
                                        }
                                    }

                                    cellValues[index++] = NoHtml(taskCell.Value);//taskCell.Descendants(ns + "T").First().Value;
                                }

                                if (string.IsNullOrEmpty(strTaskId))
                                {
                                    continue;
                                }

                                // 新建任务
                                TaskItem newTask = new TaskItem();

                                newTask.NoteBookId      = notebookNode.Attribute("ID").Value;
                                newTask.NoteBookName    = notebookNode.Attribute("name").Value;
                                newTask.SectionId = sectionNode.Attribute("ID").Value;
                                newTask.SectionName = sectionNode.Attribute("name").Value;
                                newTask.PageId = pageNode.Attribute("ID").Value;
                                newTask.PageName = pageNode.Attribute("name").Value;

                                newTask.TaskOneNoteId = strTaskId;
                                newTask.TaskComplete = bTaskComplete;
                                newTask.TaskName = cellValues[0];
                                int value = 0;
                                int.TryParse(cellValues[1], out value);
                                newTask.TaskTimeEdit = value;
                                value = 0;
                                int.TryParse(cellValues[2], out value);
                                newTask.TaskTimeUsage = value;
                                value = 0;
                                int.TryParse(cellValues[3], out value);
                                newTask.TaskTimeInterrupt = value;
                                newTask.TaskComment = cellValues[4];

                                if (tagDefIndex != -1)
                                {
                                    newTask.TaskClass = TagDefsDic[tagDefIndex];
                                }

                                taskList.Add(newTask);
                            }
                        }
                    }
                }
            }

            return taskList;
        }
        
        public static void CreateTaskItem()
        {
            string notebookXml;
            var onenoteApp = new Microsoft.Office.Interop.OneNote.Application();
            onenoteApp.GetHierarchy(null, HierarchyScope.hsPages, out notebookXml);

            var doc = System.Xml.Linq.XDocument.Parse(notebookXml);
            var ns = doc.Root.Name.Namespace;

            XElement oe = new XElement(ns + "OE");
            oe.SetAttributeValue("objectID", "{1070E934-A60F-0F9F-3352-98A3F112008F}{46}{B0}");
            oe.Add(new XElement(ns + "T",
                      new XCData("123")));

            var page = new XDocument(new XElement(ns + "Page",
                                 new XElement(ns + "Outline",
                                   new XElement(ns + "OEChildren",
                                     oe))));
            //var page = new XDocument(new XElement(ns + "Page",
            //                            new XElement(ns + "OEChildren",
            //                              oe)));

            page.Root.SetAttributeValue("ID", "{60FE03D2-2EB9-4049-971D-00AE34EAAD3B}{1}{E178567790977931809320135678447047691353931}");
            page.Root.Element(ns + "Outline").SetAttributeValue("objectID", "{1070E934-A60F-0F9F-3352-98A3F112008F}{15}{B0}");
            onenoteApp.UpdatePageContent(page.ToString(), DateTime.MinValue);
        }

        public static bool UpdateTaskItem(TaskItem newTask)
        {
            string notebookXml;
            var onenoteApp = new Microsoft.Office.Interop.OneNote.Application();
            onenoteApp.GetHierarchy(null, HierarchyScope.hsPages, out notebookXml);

            var doc = System.Xml.Linq.XDocument.Parse(notebookXml);
            var ns = doc.Root.Name.Namespace;

            var notebookNode = doc.Descendants(ns + "Notebook").Where(n =>
                    n.Attribute("ID").Value.Equals(newTask.NoteBookId)).FirstOrDefault();
            if (notebookNode==null)
            {
                return false;
            }

            var sectionNode = notebookNode.Descendants(ns + "Section").Where(n =>
                        n.Attribute("ID").Value.Equals(newTask.SectionId)).FirstOrDefault();
            if (sectionNode == null)
            {
                return false;
            }

            var pageNode = sectionNode.Descendants(ns + "Page")
                        .Where(n => n.Attribute("ID").Value.Equals(newTask.PageId))
                        .FirstOrDefault();
            if (pageNode != null)
            {
                string pageXml;
                onenoteApp.GetPageContent(pageNode.Attribute("ID").Value, out pageXml);
                var pageDoc = System.Xml.Linq.XDocument.Parse(pageXml);
                var taskTable = pageDoc.Descendants(ns + "Table").FirstOrDefault();
                if (taskTable != null)
                {
                    bool bfirst = true;
                    foreach (var taskRow in from row in taskTable.Descendants(ns + "Row") select row)
                    {
                        if (bfirst)
                        {
                            bfirst = false;
                            continue;
                        }

                        string strTaskId = "";
                        var taskCells = taskRow.Descendants(ns + "Cell");
                        if (taskCells.Count() != 5)
                        {
                            continue;
                        }

                        // 判断任务ID是否相同
                        var taskId = taskCells.ElementAt(0).Descendants(ns + "OutlookTask").FirstOrDefault();
                        if (taskId == null)
                        {
                            continue;
                        }

                        strTaskId = taskId.Attribute("guidTask").Value;
                        if (strTaskId != newTask.TaskOneNoteId)
                        {
                            continue;
                        }

                        // 更新任务信息到OneNote文件中
                        //taskCells.ElementAt(0).SetValue(newTask.TaskName);
                        taskId.SetAttributeValue("completed", newTask.TaskComplete);
                        if (newTask.TaskTimeEdit > 0)
                        {
                            taskCells.ElementAt(1).Descendants(ns + "T").FirstOrDefault().SetValue(
                                new XCData(newTask.TaskTimeEdit.ToString()).Value);
                        }
                        
                        if (newTask.TaskTimeUsage>0)
                        {
                            taskCells.ElementAt(2).Descendants(ns + "T").FirstOrDefault().SetValue(
                                new XCData(newTask.TaskTimeUsage.ToString()).Value);
                        }
                        
                        if (newTask.TaskTimeInterrupt > 0)
                        {
                            taskCells.ElementAt(3).Descendants(ns + "T").FirstOrDefault().SetValue( 
                                new XCData(newTask.TaskTimeInterrupt.ToString()).Value);
                        }

                        taskCells.ElementAt(4).Descendants(ns + "T").FirstOrDefault().SetValue(
                                new XCData(newTask.TaskComment).Value);
                        onenoteApp.UpdatePageContent(pageDoc.ToString(), DateTime.MinValue);
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool UpdateTaskList(string notebook, string section, string page, TaskItem newTask)
        {
            string notebookXml;
            var onenoteApp = new Microsoft.Office.Interop.OneNote.Application();
            onenoteApp.GetHierarchy(null, HierarchyScope.hsPages, out notebookXml);

            var doc = System.Xml.Linq.XDocument.Parse(notebookXml);
            var ns = doc.Root.Name.Namespace;

            var notebookNodeList = doc.Descendants(ns + "Notebook").Where(n =>
                    n.Attribute("name").Value.Contains(notebook));
            if (notebookNodeList == null)
            {
                return false;
            }

            foreach (var notebookNode in notebookNodeList)
            {
                var sectionNode = notebookNode.Descendants(ns + "Section").Where(n =>
                        n.Attribute("name").Value.Contains(section)).FirstOrDefault();
                if (sectionNode != null)
                {
                    var pageNode = sectionNode.Descendants(ns + "Page")
                                  .Where(n => n.Attribute("name").Value.Contains(page))
                                  .FirstOrDefault();
                    if (pageNode != null)
                    {
                        
                        string pageXml;
                        onenoteApp.GetPageContent(pageNode.Attribute("ID").Value, out pageXml);
                        var pageDoc = XDocument.Parse(pageXml);
                        var taskTable = pageDoc.Descendants(ns + "Table").FirstOrDefault();
                        if (taskTable != null)
                        {
                            bool bfirst = true;
                            foreach (var taskRow in from row in taskTable.Descendants(ns + "Row") select row)
                            {
                                if (bfirst)
                                {
                                    bfirst = false;
                                    continue;
                                }
                               
                                string strTaskId = "";
                                var taskCells = taskRow.Descendants(ns + "Cell");
                                if (taskCells.Count() == 5)
                                {
                                    var taskId = taskCells.ElementAt(0).Descendants(ns + "OutlookTask").FirstOrDefault();
                                    if (taskId == null)
                                    {
                                        continue;
                                    }
                                    strTaskId = taskId.Attribute("guidTask").Value;
                                    if (strTaskId != newTask.TaskOneNoteId)
                                    {
                                        continue;
                                    }

                                    if (newTask.TaskTimeEdit > 0)
                                    {
                                        taskCells.ElementAt(1).Value = newTask.TaskTimeEdit.ToString();
                                    }
                                    if (newTask.TaskTimeUsage > 0)
                                    {
                                        taskCells.ElementAt(2).Value = newTask.TaskTimeUsage.ToString();
                                    }

                                    string strModifiedPage = pageDoc.ToString();

                                    onenoteApp.UpdatePageContent(strModifiedPage, DateTime.MinValue);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
