﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;
// ReSharper disable AssignNullToNotNullAttribute

namespace MovieSorter
{
    public partial class MovieSorter : Form
    {
        public MovieSorter()
        {
            InitializeComponent();
        }

        private void MovieSorter_Load(object sender, EventArgs e)
        {
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.Columns.Add("", 24);
            listView1.Columns.Add("#");
            listView1.Columns.Add("Folder");
            listView1.CheckBoxes = true;
        }


        private void Counter(List<string> match)
        {
            if (match.Count > 1 || match.Count == 0) Count.Text = match.Count + @" Items in MatchList";
            else Count.Text = match.Count + @" Item in MatchList";
        }

        private void Add(string box, string index, string path)
        {
            progressBar1.PerformStep();
            string[] row = { box, index, path };
            var item = new ListViewItem(row);
            listView1.Items.Add(item);

        }

        private void Browes_Source_Click(object sender, EventArgs e)
        {
            var fs = new FolderSelectDialog();
            var result = fs.ShowDialog();
            if (!result) return;
            Source_dir.Text = fs.FileName;
        }




        private void Query()
        {
            var filters =
                new Regex(
                    @"(CAMRip|CAM|TS|TELESYNC|PDVD|PTVD|PPVRip|SCR|SCREENER|DVDSCR|DVDSCREENER|BDSCR|R4|R5|R5LINE|R5.LINE|DVD|DVD5|DVD9|DVDRip|DVDR|TVRip|DSR|PDTV|SDTV|HDTV|HDTVRip|DVB|DVBRip|DTHRip|VODRip|VODR|BDRip|BRRip|BR.Rip|BluRay|Blu.Ray|BD|BDR|BD25|BD50|3D.BluRay|3DBluRay|3DBD|Remux|BDRemux|BR.Scr|BR.Screener|HDDVD|HDRip|WorkPrint|VHS|VCD|TELECINE|WEBRip|WEB.Rip|WEBDL|WEB.DL|WEBCap|WEB.Cap|ithd|iTunesHD|Laserdisc|AmazonHD|NetflixHD|NetflixUHD|VHSRip|LaserRip|URip|UnknownRip|MicroHD|WP|TC|PPV|DDC|R5.AC3.5.1.HQ|DVD-Full|DVDFull|Full-Rip|FullRip|DSRip|SATRip|BD5|BD9|Extended|Uncensored|Remastered|Unrated|Uncut|IMAX|(Ultimate.)?(Director.?s|Theatrical|Ultimate|Final|Rogue|Collectors|Special|Despecialized).(Cut|Edition|Version)|((H|HALF|F|FULL)[^\\p{Alnum}]{0,2})?(SBS|TAB|OU)|DivX|Xvid|AVC|(x|h)[.]?(264|265)|HEVC|3ivx|PGS|MP[E]?G[45]?|MP[34]|(FLAC|AAC|AC3|DD|MA).?[2457][.]?[01]|[26]ch|(Multi.)?DTS(.HD)?(.MA)?|FLAC|AAC|AC3|TrueHD|Atmos|[M0]?(420|480|720|1080|1440|2160)[pi]|(?<=[-.])(420|480|720|1080|2D|3D)|10.?bit|(24|30|60)FPS|Hi10[P]?|[a-z]{2,3}.(2[.]0|5[.]1)|(19|20)[0-9]+(.)S[0-9]+(?!(.)?E[0-9]+)|(?<=\\d+)v[0-4]|CD\\d+|3D|2D)");
            const string baseUrl = "http://www.omdbapi.com/";
            listView1.Items.Clear();
            var match = new List<string>();
            var ignore = new List<string>();
            if (Directory.Exists(Source_dir.Text))
            {
                var allfiles = GetFiles(Source_dir.Text, "*.*");
                var i = 1;
                foreach (var name in allfiles)
                {
                    var potential = new List<dynamic>();
                    var dir = Path.GetDirectoryName(name);
                    if (match.Contains(dir) || ignore.Contains(dir) || match.Contains(name)) continue;
                    var ext = Path.GetExtension(name);
                    if (ext == null) continue;
                    ext = ext.ToLower();
                    if (!ext.Equals(".mp4") && !ext.Equals(".avi") && !ext.Equals(".mkv")) continue;
                    var file = Path.GetFileNameWithoutExtension(name);
                    var sp = new Regex("[sS][0-9]{2}[eE][0-9]{2}");
                    if (file != null && sp.IsMatch(file))
                    {
                        const string sPattern = "[sS][0-9]{2}";
                        var s = Regex.Match(file, sPattern);
                        var series = s.Value.ToLower();
                        series = series.Replace("s", "");
                        sp = new Regex("[sS][0-9]{2}[eE][0-9]{2}.*");
                        var ep = sp.Replace(file, string.Empty);
                        var last = ep[ep.Length - 1];
                        if (last.Equals('.')) ep = ep.Substring(0, ep.Length - 1);
                        var uri = baseUrl + "?t=" + ep + "&Season=" + series + "&r=json";
                        var request = WebRequest.Create(uri);
                        request.Credentials = CredentialCache.DefaultCredentials;
                        var response = request.GetResponse();
                        // Get the stream containing content returned by the server.
                        var dataStream = response.GetResponseStream();
                        // Open the stream using a StreamReader for easy access.
                        if (dataStream == null) continue;
                        var reader = new StreamReader(dataStream);
                        // Read the content.
                        var responseFromServer = reader.ReadToEnd();
                        // Display the content.
                        dynamic result = JsonConvert.DeserializeObject<dynamic>(responseFromServer);
                        var episodes = result.Episodes;
                        // Clean up the streams and the response.
                        reader.Close();
                        response.Close();
                        if (episodes == null)
                        {
                            MessageBox.Show(file);
                            continue;
                        }
                        if (TestYear(episodes))
                        {
                            match.Add(dir);
                            progressBar1.Visible = true;
                            progressBar1.Minimum = 0;
                            progressBar1.Maximum = match.Count;
                            progressBar1.Value = 0;
                            progressBar1.Step = 1;
                        }
                        else ignore.Add(dir);
                    }

                    else
                    {
                        const string mPattern = "[0-9]{4}";
                        var input = file;
                        var m = Regex.Match(input, mPattern);
                        var sDate = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                        var datevalue = Convert.ToDateTime(sDate);
                        var year = !m.Success ? 0 : int.Parse(m.Value);
                        if (year >= 1889 && year < datevalue.Year)
                        {
                            if (year == int.Parse(Year_TextBox.Text)) match.Add(name);
                            progressBar1.Maximum = match.Count;

                        }
                        else
                        {
                            var tmp = file.Split('.');
                            var tmpName = "";
                            foreach (var item in tmp)
                            {
                                if (TestWord(item, filters)) tmpName += item + ".";
                                else break;
                            }
                            tmpName = tmpName.Substring(0, tmpName.Length - 1);
                            var uri = baseUrl + "?s=" + tmpName + "&type=movie&r=json";
                            tmpName = tmpName.Replace(".", " ");
                            var request = WebRequest.Create(uri);
                            request.Credentials = CredentialCache.DefaultCredentials;
                            var response = request.GetResponse();
                            var dataStream = response.GetResponseStream();
                            var reader = new StreamReader(dataStream);
                            var responseFromServer = reader.ReadToEnd();
                            dynamic result = JsonConvert.DeserializeObject<dynamic>(responseFromServer);
                            string tmpResult = result.ToString();
                            response.Close();
                            dataStream.Close();
                            reader.Close();
                            if (result.Response == "False") continue;
                            {
                                tmpResult = tmpResult.Replace("\r\n", string.Empty);
                                var start = tmpResult.IndexOf("[", StringComparison.Ordinal);
                                tmpResult = tmpResult.Substring(start, tmpResult.Length - start);
                                var end = tmpResult.LastIndexOf("]", StringComparison.Ordinal) + 1;
                                tmpResult = tmpResult.Substring(0, end);
                                //tmpResult = tmpResult.Remove(tmpResult.Trim().Length - 1);
                                result = JsonConvert.DeserializeObject<dynamic>(tmpResult);
                                foreach (var item in result)
                                {
                                    if (item.Title.ToString().Equals(tmpName) &&
                                        item.Type.ToString().Equals("movie")) potential.Add(item);
                                }
                                if (potential.Count == 1)
                                {
                                    if (potential[0].Year.ToString() == Year_TextBox.Text)
                                    {
                                        match.Add(name);
                                        progressBar1.Maximum = match.Count;
                                    }

                                }

                                else if (potential.Count != 0)
                                {
                                    var msg = new MsgBox();
                                    const string imdburl = "http://www.imdb.com/title/";
                                    const int x = 25;
                                    var y = 30;
                                    foreach (var potentialItem in potential)
                                    {

                                        //imdbID = imdburl + potentialItem.imdbID.ToString();
                                        msg.AddLabeles(imdburl + potentialItem.imdbID.ToString(), potentialItem.Title.ToString() + " (" + potentialItem.Year.ToString() + ")", x, y);
                                        y += 20;
                                    }
                                    msg.MyTitle = tmpName;
                                    msg.ShowDialog();
                                    foreach (var selected in potential)
                                    {
                                        if (!selected.imdbID.ToString().Equals(msg.MyId)) continue;
                                        if (!selected.Year.ToString().Equals(Year_TextBox.Text)) continue;
                                        match.Add(name);
                                        progressBar1.Maximum = match.Count;
                                    }
                                }
                            }
                        }

                    }
                }


                foreach (var film in match)
                {
                    var co = i++;
                    Add("", co.ToString(), film);

                }
                listView1.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
                listView1.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.HeaderSize);
                listView1.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
                if (listView1.Items.Count == 0)
                    listView1.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.HeaderSize);
                Counter(match);
            }

            else MessageBox.Show(@"No such Folder");

        }

        private static bool TestWord(string arg, Regex filters)
        {
            return !filters.IsMatch(arg);
        }

        private bool TestYear(dynamic episodes)
        {
            foreach (var episode in episodes)
            {
                string releas = episode.Released.ToString();
                if (releas.Contains(Year_TextBox.Text)) return true;

            }
            return false;
        }

        private static IEnumerable<string> GetFiles(string path, string pattern)
        {
            var files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch
            {
                Console.WriteLine(@"Opps!");
            }

            return files;
        }

        private void Move_button_Click(object sender, EventArgs e)
        {
            var ignore = new List<string>();
            var list = new List<string>();
            if (Source_dir.Text == "")
            {
                errorProvider1.SetIconPadding(Move_button, -90);
                errorProvider1.SetError(Move_button, "Set Source dir first");
            }
            else if (Distension_dir.Text == "")
            {
                errorProvider1.SetIconPadding(Move_button, -90);
                errorProvider1.SetError(Move_button, "Set Distension dir first");
            }
            else
            {
                foreach (ListViewItem item in listView1.Items)
                {
                    if (!item.Checked) continue;
                    try
                    {
                        var baseDir = Source_dir.Text;
                        var source = item.SubItems[2].Text;
                        var size = baseDir.Length;
                        var part = source.Remove(0, size);
                        var dest = Distension_dir.Text + part;
                        var attr = File.GetAttributes(item.SubItems[2].Text);
                        if (attr != FileAttributes.Directory)
                        {
                            if (Directory.Exists(Path.GetDirectoryName(dest)))
                            {
                                FileSystem.MoveFile(source, dest, UIOption.AllDialogs);
                                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(source)))
                                {
                                    var extension = Path.GetExtension(file);
                                    var fileNoExt = Path.GetFileNameWithoutExtension(file);
                                    if (!source.ToLower().Contains(fileNoExt.ToLower())) continue;
                                    var destfile = Path.GetDirectoryName(dest) + "\\" + Path.GetFileName(file);
                                    if (extension != null && extension.Equals(".srt")) FileSystem.MoveFile(file, destfile, UIOption.AllDialogs);
                                }

                                ignore.Add(item.SubItems[2].Text);
                            }


                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                                FileSystem.MoveFile(source, dest, UIOption.AllDialogs);
                                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(source)))
                                {
                                    var extension = Path.GetExtension(file);
                                    var destFile = Path.GetDirectoryName(dest) + "\\" + Path.GetFileName(file);
                                    if (extension == null || !extension.Equals(".srt")) continue;
                                    FileSystem.MoveFile(file, destFile, UIOption.AllDialogs);
                                }
                                if (Directory.GetFileSystemEntries(Path.GetDirectoryName(source)).Length == 0) Directory.Delete(Path.GetDirectoryName(source));
                                ignore.Add(item.SubItems[2].Text);
                            }
                        }
                        else
                        {
                            FileSystem.MoveDirectory(source, dest, UIOption.AllDialogs);
                            ignore.Add(item.SubItems[2].Text);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                var i = 1;
                list.AddRange(from ListViewItem item in listView1.Items select item.SubItems[2].Text);
                listView1.Items.Clear();
                foreach (var ig in list)
                {
                    if (ignore.Contains(ig)) continue;
                    var co = i++;
                    Add("", co.ToString(), ig);
                }
                var num = list.Count - ignore.Count;
                listView1.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
                listView1.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.HeaderSize);
                listView1.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
                listView1.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.HeaderSize);
                if (num > 1 || num == 0) Count.Text = num + @" Items in MatchList";
                else Count.Text = num + @" Item in MatchList";
            }
        }

        private void Browes_Destination_Click(object sender, EventArgs e)
        {
            var fs = new FolderSelectDialog();
            var result = fs.ShowDialog();
            if (!result) return;
            Distension_dir.Text = fs.FileName;
        }

        private void Check_All_CheckedChanged(object sender, EventArgs e)
        {
            if (Check_All.Checked)
            {
                for (var i = 0; i < listView1.Items.Count; i++)
                {
                    listView1.Items[i].Checked = true;
                }
            }
            else
            {
                for (var i = 0; i < listView1.Items.Count; i++)
                {
                    listView1.Items[i].Checked = false;
                }
            }
        }

        private void Go_button_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            Query();
        }

        private void Source_dir_TextChanged(object sender, EventArgs e)
        {
            if (Source_dir.Text == "" || Distension_dir.Text == "" || _error)
            {
                Go_button.Enabled = false;
                Move_button.Enabled = false;
            }

            else
            {
                Go_button.Enabled = true;
                Move_button.Enabled = true;
            }
        }

        private void Distension_dir_TextChanged(object sender, EventArgs e)
        {
            if (Source_dir.Text == "" || Distension_dir.Text == "" || _error)
            {
                Go_button.Enabled = false;
                Move_button.Enabled = false;
            }

            else
            {
                Go_button.Enabled = true;
                Move_button.Enabled = true;
            }
        }

        private static bool _error = true;
        private void Year_TextBox_TextChanged(object sender, EventArgs e)
        {

            if (Regex.IsMatch(Year_TextBox.Text, "[^0-9]"))
            {
                Year_TextBox.Text = Year_TextBox.Text.Substring(0, Year_TextBox.Text.Length - 1);
                Year_TextBox.Focus();
                Year_TextBox.SelectionStart = Year_TextBox.Text.Length;
            }

            if (Year_TextBox.TextLength == 4)
            {
                var sDate = DateTime.Now.ToString(CultureInfo.CurrentCulture);
                var datevalue = Convert.ToDateTime(sDate);
                var year = int.Parse(Year_TextBox.Text);
                if (year > datevalue.Year || year < 1889)
                {
                    errorProvider1.SetError(Year_TextBox, "The Year Most be Between 1889 - " + datevalue.Year);
                    Go_button.Enabled = false;
                    Move_button.Enabled = false;
                    _error = true;
                }
                else if (Distension_dir.Text != "" && Source_dir.Text != "")
                {
                    errorProvider1.Clear();
                    Go_button.Enabled = true;
                    Move_button.Enabled = true;
                    _error = false;

                }
                else
                {
                    errorProvider1.Clear();
                    _error = false;
                }
            }

            else
            {
                errorProvider1.Clear();
                Go_button.Enabled = false;
                Move_button.Enabled = false;
                _error = true;
            }
        }
    }
}
