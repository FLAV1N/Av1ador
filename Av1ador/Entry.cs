﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Av1ador
{
    public class Entry
    {
        private const string queue = "queue.xml";
        private const string backup = "queue.xml.bak";
        private static bool shouldsave;
        private static int lastupdate;
        private static int refresh;
        public static object[] elapsed_add;
        public static int Lastsave { get; set; }
        public string File { get; set; }
        public int Status { get; set; }
        public int Elapsed { get; set; }
        public string Vf { get; set; }
        public string Af { get; set; }
        public string Gs { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double Credits { get; set; }
        public double CreditsEnd { get; set; }
        public string Cv { get; set; }
        public string Bits { get; set; }
        public string Param { get; set; }
        public int Crf { get; set; }
        public int Ba { get; set; }
        public string Bv { get; set; }
        public int Track { get; set; }
        public string Resolution { get; set; }
        public string Speed { get; set; }


        public static int Index(string file, ListBox list)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                Entry entry = list.Items[i] as Entry;
                if (file == "-1" && (entry.Status == 1 || (entry.Elapsed > 0 && entry.Status == 0)))
                    return i;
                if (entry.File == file)
                    return i;
            }
            return -1;
        }

        public static Brush Brush_bg(bool selected, int index, int status = 0)
        {
            if (status > 1)
                return Brushes.LightGreen;
            else if (status < 0)
                return Brushes.LightCoral;
            return selected ? Brushes.LightSteelBlue : (index % 2 == 0 ? Brushes.OldLace : Brushes.White);
        }

        public static void Draw(ListBox list, DrawItemEventArgs e, TimeSpan ts)
        {
            bool isItemSelected = ((e.State & DrawItemState.Selected) == DrawItemState.Selected);
            if (e.Index >= 0 && e.Index < list.Items.Count)
            {
                e.Graphics.FillRectangle(Brush_bg(isItemSelected, e.Index), e.Bounds);
                Entry entry = (Entry)list.Items[e.Index];
                if (entry.Status == 1)
                    e.Graphics.DrawRectangle(new Pen(SystemColors.Highlight, 1), e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                string dir = Path.GetDirectoryName(entry.File);
                string name = Path.GetFileName(entry.File);
                int limit = e.Bounds.Width * 3 / 7;
                if (entry.File.Length > limit)
                {
                    int dif = entry.File.Length - limit + 3;
                    dir = "..." + dir.Substring(dir.Length > dif ? dif : dir.Length - 1);
                    dif = (dir + name).Length - limit;
                    if (dif > 0)
                        name = name.Substring(0, name.Length > dif ? name.Length - dif : name.Length) + "...";
                }
                e.Graphics.DrawString((dir + "\\" + name).Replace(@"\\",@"\"), e.Font, Brushes.Black, e.Bounds);
                if (entry.Elapsed > 0 || entry.Status == 1)
                {
                    int x = e.Bounds.Right - 52;
                    int y = e.Bounds.Bottom - 16;
                    e.Graphics.FillRectangle(Brush_bg(isItemSelected, e.Index, entry.Status), x, y, 51, 15);
                    TimeSpan t = (entry.Status == 1 ? ts - TimeSpan.FromMilliseconds(Lastsave) : new TimeSpan(0)) + TimeSpan.FromMilliseconds(entry.Elapsed);
                    e.Graphics.DrawString("[" + ((int)t.TotalHours).ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00") + "]", e.Font, Brushes.Black, x, y);
                }
            }
        }

        private static void Elapsed_save(ListBox list, TimeSpan ts)
        {
            int add = elapsed_add != null ? Convert.ToInt32(elapsed_add[0]) : 0;
            if (add == 0)
                return;
            for (int i = 0; i < list.Items.Count; i++)
            {
                Entry entry = list.Items[i] as Entry;
                try
                {
                    if ((string)elapsed_add[1] == entry.File)
                    {
                        entry.Elapsed += add;
                        elapsed_add = null;
                        Save_entries(list);
                        Lastsave = (int)ts.TotalMilliseconds;
                    }
                } catch { elapsed_add = null; }
            }
        }

        public static void Set_status(ListBox list, string file, TimeSpan ts, bool running = false, bool failed = false, bool finished = false, bool restart = false)
        {
            lastupdate = (int)(ts.TotalMilliseconds > 0 ? ts.TotalMilliseconds : lastupdate);
            for (int i = 0; i < list.Items.Count; i++)
            {
                Entry entry = list.Items[i] as Entry;
                if (entry.File == file)
                {
                    if (restart)
                        entry.Elapsed = Lastsave = 0;
                    int status = entry.Status;
                    entry.Status = running ? 1 : failed ? -1 : finished ? 2 : status != -1 && status != 2 ? 0 : entry.Status;
                    if (status != entry.Status)
                    {
                        if (entry.Status == 2 || entry.Status == -1 || (status == 1 && entry.Status == 0))
                        {
                            entry.Elapsed += lastupdate > Lastsave ? lastupdate - Lastsave : 0;
                            Lastsave = 0;
                        }
                        Save(list, true);
                        if (!running && entry.Status != 1)
                            list.Refresh();
                    }
                }
            }
            if (running)
                refresh--;
            if (refresh < 1)
            {
                refresh = 10;
                list.Refresh();
                if (running)
                    Elapsed_save(list, ts);
            }
        }

        public static void Save(ListBox list, bool save = false)
        {
            if (shouldsave || save)
            {
                shouldsave = false;
                Save_entries(list);
            }
        }

        public static void Load(ListBox list)
        {
            if (System.IO.File.Exists(queue))
            {
                System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(typeof(Entry[]));
                StreamReader file = new StreamReader(queue);
                Entry[] entries;
                try
                {
                    entries = (Entry[])reader.Deserialize(file);
                    file.Close();
                    foreach (Entry entry in entries)
                        list.Items.Add(entry);
                }
                catch
                {
                    if (System.IO.File.Exists(backup))
                    {
                        file = new StreamReader(backup);
                        entries = (Entry[])reader.Deserialize(file);
                        file.Close();
                        foreach (Entry entry in entries)
                            list.Items.Add(entry);
                    }
                }
            }
        }

        public static void Update(int col, Video video, ListBox list, ListBox vf, ListBox af, string gs, string cv, string bits, string param, int crf, int ba, string bv, int track, string res, string spd)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                Entry entry = list.Items[i] as Entry;
                if (video.File != "" && entry.File == video.File)
                {
                    switch (col)
                    {
                        case 1:
                            string j = String.Join("¡", vf.Items.OfType<string>().ToArray());
                            string k = String.Join("¡", af.Items.OfType<string>().ToArray());
                            shouldsave = j != entry.Vf || k != entry.Af;
                            if (shouldsave)
                            {
                                entry.Vf = j;
                                entry.Af = k;
                            }
                            break;
                        case 2:
                            shouldsave = gs != entry.Gs;
                            if (shouldsave)
                                entry.Gs = gs;
                            break;
                        case 3:
                            shouldsave = video.CreditsTime != entry.Credits || video.CreditsEndTime != entry.CreditsEnd;
                            if (shouldsave)
                            {
                                entry.Credits = video.CreditsTime;
                                entry.CreditsEnd = video.CreditsEndTime;
                            }
                            break;
                        case 4:
                            shouldsave = cv != entry.Cv;
                            if (shouldsave)
                            {
                                entry.Cv = cv;
                                entry.Param = "";
                            }
                            break;
                        case 5:
                            shouldsave = bits != entry.Bits;
                            if (shouldsave)
                                entry.Bits = bits;
                            break;
                        case 6:
                            shouldsave = param != entry.Param;
                            if (shouldsave)
                                entry.Param = param;
                            break;
                        case 7:
                            shouldsave = crf != entry.Crf;
                            if (shouldsave)
                                entry.Crf = crf;
                            break;
                        case 8:
                            shouldsave = ba != entry.Ba;
                            if (shouldsave)
                                entry.Ba = ba;
                            break;
                        case 9:
                            shouldsave = bv != entry.Bv;
                            if (shouldsave)
                                entry.Bv = bv;
                            break;
                        case 10:
                            shouldsave = track != entry.Track;
                            if (shouldsave)
                                entry.Track = track;
                            break;
                        case 11:
                            shouldsave = res != entry.Resolution;
                            if (shouldsave)
                                entry.Resolution = res;
                            break;
                        case 12:
                            shouldsave = spd != entry.Speed;
                            if (shouldsave)
                                entry.Speed = spd;
                            break;
                        case 13:
                            shouldsave = video.StartTime != entry.Start || video.EndTime != entry.End;
                            if (shouldsave)
                            {
                                entry.Start = video.StartTime;
                                entry.End = video.EndTime;
                            }
                            break;
                    }
                    list.Items[i] = entry;
                }
            }
        }

        private static void Save_entries(ListBox list)
        {
            if (System.IO.File.Exists(backup))
                System.IO.File.Delete(backup);
            if (System.IO.File.Exists(queue))
                System.IO.File.Move(queue, backup);
            var writer = new System.Xml.Serialization.XmlSerializer(typeof(Entry[]));
            var wfile = new StreamWriter(queue);
            Entry[] entries = new Entry[list.Items.Count];
            for (int i = 0; i < list.Items.Count; i++)
                entries[i] = list.Items[i] as Entry;
            writer.Serialize(wfile, entries);
            wfile.Close();
        }

        public static List<string> Filter2List(string f)
        {
            return f.Split(new char[] { '¡' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static bool Queued(ListBox list, string file)
        {
            for (int i = 0; i < list.Items.Count; i++)
            {
                if ((list.Items[i] as Entry).File == file)
                    return true;
            }
            return false;
        }
    }
}
