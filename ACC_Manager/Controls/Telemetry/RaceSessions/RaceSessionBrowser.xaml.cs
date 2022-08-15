﻿using ACCManager.Broadcast;
using ACCManager.Controls.Telemetry.RaceSessions;
using ACCManager.Data;
using ACCManager.Data.ACC.Database.GameData;
using ACCManager.Data.ACC.Database.LapDataDB;
using ACCManager.Data.ACC.Database.SessionData;
using ACCManager.Data.ACC.Tracks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static ACCManager.Data.ACC.Tracks.TrackNames;

namespace ACCManager.Controls
{
    /// <summary>
    /// Interaction logic for RaceSessionBrowser.xaml
    /// </summary>
    public partial class RaceSessionBrowser : UserControl
    {
        public RaceSessionBrowser()
        {
            InitializeComponent();

            //this.Loaded += (s, e) => FillTrackComboBox();

            comboTracks.SelectionChanged += (s, e) => FillCarComboBox();
            comboCars.SelectionChanged += (s, e) => LoadSessionList();
            listViewRaceSessions.SelectionChanged += (s, e) => LoadSession();
        }

        private void LoadSession()
        {
            DbRaceSession session = GetSelectedRaceSession();
            if (session == null) return;

            Dictionary<int, DbLapData> laps = LapDataCollection.GetForSession(session.Id);
            stackerSessionViewer.Children.Clear();

            if (session == null) return;

            stackerSessionViewer.Children.Add(new TextBlock()
            {
                Text = $"{ACCSharedMemory.SessionTypeToString(session.SessionType)} - {(session.IsOnline ? "On" : "Off")}line"
            });
            stackerSessionViewer.Children.Add(new TextBlock()
            {
                Text = $"Session Index: {session.SessionIndex}"
            });
            session.UtcStart = DateTime.SpecifyKind(session.UtcStart, DateTimeKind.Utc);
            session.UtcEnd = DateTime.SpecifyKind(session.UtcEnd, DateTimeKind.Utc);
            stackerSessionViewer.Children.Add(new TextBlock()
            {
                Text = $"Start: {session.UtcStart.ToLocalTime():U} \nEnd: {session.UtcEnd.ToLocalTime():U}"
            });

            int potentialBestLapTime = laps.GetPotentialFastestLapTime();
            stackerSessionViewer.Children.Add(new TextBlock()
            {
                Text = $"Potential best: {new TimeSpan(0, 0, 0, 0, potentialBestLapTime):mm\\:ss\\:fff}"
            });

            stackerSessionViewer.Children.Add(GetLapDataGrid(laps));
        }

        public DataGrid GetLapDataGrid(Dictionary<int, DbLapData> laps)
        {
            var data = laps.OrderByDescending(x => x.Key).Select(x => x.Value);
            DataGrid grid = new DataGrid()
            {
                Height = 550,
                ItemsSource = data,
                AutoGenerateColumns = false,
                CanUserDeleteRows = false,
                CanUserAddRows = false,
                IsReadOnly = true,
                EnableRowVirtualization = false,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                GridLinesVisibility = DataGridGridLinesVisibility.Vertical,
                AlternatingRowBackground = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0)),
                RowBackground = Brushes.Transparent,
            };

            int fastestLapIndex = laps.GetFastestLapIndex();
            grid.LoadingRow += (s, e) =>
            {
                DataGridRowEventArgs ev = e;
                DbLapData lapData = (DbLapData)ev.Row.DataContext;

                if (!lapData.IsValid)
                    ev.Row.Foreground = Brushes.OrangeRed;

                if (lapData.Index == fastestLapIndex)
                    ev.Row.Foreground = Brushes.LimeGreen;

                switch (lapData.LapType)
                {
                    case LapType.Outlap:
                        {
                            ev.Row.FontStyle = FontStyles.Italic;
                            break;
                        }
                    case LapType.Inlap:
                        {
                            ev.Row.FontStyle = FontStyles.Italic;
                            break;
                        }
                }
            };

            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Lap",
                Binding = new Binding("Index"),
                SortDirection = System.ComponentModel.ListSortDirection.Descending,
                FontWeight = FontWeights.DemiBold,
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Time",
                Binding = new Binding("Time") { Converter = new MillisecondsToFormattedTimeSpanString() }
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Sector 1",
                Binding = new Binding("Sector1") { Converter = new DivideBy1000ToFloatConverter() }
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Sector 2",
                Binding = new Binding("Sector2") { Converter = new DivideBy1000ToFloatConverter() }
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Sector 3",
                Binding = new Binding("Sector3") { Converter = new DivideBy1000ToFloatConverter() }
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Fuel Used",
                Binding = new Binding("FuelUsage") { Converter = new DivideBy1000ToFloatConverter() }
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Fuel in tank",
                Binding = new Binding("FuelInTank")
            });
            grid.Columns.Add(new DataGridTextColumn()
            {
                Header = "Type",
                Binding = new Binding("LapType")
            });


            return grid;
        }

        private Guid GetSelectedTrack()
        {
            if (comboTracks.SelectedIndex == -1) return Guid.Empty;
            return (Guid)(comboTracks.SelectedItem as ComboBoxItem).DataContext;
        }

        private Guid GetSelectedCar()
        {
            if (comboCars.SelectedIndex == -1) return Guid.Empty;
            return (Guid)(comboCars.SelectedItem as ComboBoxItem).DataContext;
        }

        private DbRaceSession GetSelectedRaceSession()
        {
            if (listViewRaceSessions.SelectedIndex == -1) return null;
            return (DbRaceSession)(listViewRaceSessions.SelectedItem as ListViewItem).DataContext;
        }

        public void FillCarComboBox()
        {
            if (GetSelectedTrack() == Guid.Empty)
                return;

            List<Guid> carGuidsForTrack = RaceSessionCollection.GetAllCarsForTrack(GetSelectedTrack());
            List<DbCarData> allCars = CarDataCollection.GetAll();

            comboCars.Items.Clear();
            foreach (DbCarData carData in allCars.Where(x => carGuidsForTrack.Contains(x.Id)))
            {
                var carModel = ConversionFactory.ParseCarName(carData.ParseName);
                string carName = ConversionFactory.GetNameFromCarModel(carModel);
                ComboBoxItem item = new ComboBoxItem() { DataContext = carData.Id, Content = carName };
                comboCars.Items.Add(item);
            }
            comboCars.SelectedIndex = 0;
        }

        public void FillTrackComboBox()
        {
            comboTracks.Items.Clear();
            List<DbTrackData> allTracks = TrackDataCollection.GetAll();
            foreach (DbTrackData track in allTracks)
            {
                string trackName;
                TrackNames.Tracks.TryGetValue(track.ParseName, out TrackData trackData);
                if (trackData == null) trackName = track.ParseName;
                else trackName = trackData.FullName;

                ComboBoxItem item = new ComboBoxItem() { DataContext = track.Id, Content = trackName };
                comboTracks.Items.Add(item);
            }
            comboCars.SelectedIndex = -1;
        }

        public void LoadSessionList()
        {
            List<DbRaceSession> allsessions = RaceSessionCollection.GetAll();

            listViewRaceSessions.Items.Clear();

            foreach (DbRaceSession session in allsessions.Where(x => x.TrackId == GetSelectedTrack() && x.CarId == GetSelectedCar()))
            {
                DbCarData carData = CarDataCollection.GetCarData(session.CarId);
                DbTrackData dbTrackData = TrackDataCollection.GetTrackData(session.TrackId);

                var carModel = ConversionFactory.ParseCarName(carData.ParseName);
                string carName = ConversionFactory.GetNameFromCarModel(carModel);
                string trackName = dbTrackData.ParseName;
                TrackNames.Tracks.TryGetValue(dbTrackData.ParseName, out TrackData trackData);
                if (dbTrackData != null) trackName = trackData.FullName;

                session.UtcStart = DateTime.SpecifyKind(session.UtcStart, DateTimeKind.Utc);
                ListViewItem listItem = new ListViewItem()
                {
                    Content = $"{ACCSharedMemory.SessionTypeToString(session.SessionType)} - {session.UtcStart.ToLocalTime():U}",
                    DataContext = session
                };
                listViewRaceSessions.Items.Add(listItem);
            }
        }
    }
}
