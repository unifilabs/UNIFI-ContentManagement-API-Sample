using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ContentManagement.Entities;

namespace ContentManagement {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        // Get an access token using basic authentication (username and password)
        // Optionally create a file named "Secrets.cs" and add fields called UnifiUsername and UnifiPassword and add your credentials for the below code to work.
        // Don't forget to ensure that this file is added to your .gitignore file.
        string unifiToken = Unifi.GetAccessToken(Secrets.UnifiUsername, Secrets.UnifiPassword);

        public MainWindow() {
            InitializeComponent();

            // Hide modal windows
            gridEditParams.Visibility = Visibility.Hidden;
            gridBatchMonitor.Visibility = Visibility.Hidden;

            // Get all libraries from Unifi and display in the libraries combobox
            List<Library> libraries = Unifi.GetLibraries(unifiToken);

            // Sort the libraries by name
            libraries = libraries.OrderBy(o => o.Name).ToList();

            // Add each library to the combobox as items
            foreach (Library library in libraries)
                comboLibraries.Items.Add(library);
        }

        /// <summary>
        /// Method to execute whenever the Library selection changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboLibraries_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!(comboLibraries.SelectedItem is Library selectedLib)) { return; }

            // Get all content from the select library
            List<Content> contentList = Unifi.GetContentFromLibrary(unifiToken, selectedLib.Id);

            // Loop through all Content and retrieve Manufacturer and Model parameter data
            foreach (Content c in contentList) {
                List<Parameter> parameters = new List<Parameter>();
                parameters = c.Parameters.ToList();

                // Loop through all parameters to retrieve the Manufacturer and Model parameter values for display in DataGrid
                foreach (Parameter p in parameters) {
                    // Pass parameter values to Content object
                    if (p.Name == "Manufacturer") { c.Manufacturer = p.Value; }

                    if (p.Name == "Model") { c.Model = p.Value; }
                }
            }

            // Display list of Content objects in main DataGrid
            dataGridMain.ItemsSource = contentList;

            // Update status message
            textBoxStatus.Text = selectedLib.Name + ": " + contentList.Count().ToString();
        }

        /// <summary>
        /// Method to execute whenever Content is selected in the DataGrid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridMain_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Content selectedContent = new Content();

            // Get selected item as a Content object
            if (dataGridMain.SelectedItem != null) { selectedContent = (Content)dataGridMain.SelectedItem; }

            // Enable Edit Button if an iten is selected, hide if none
            if (dataGridMain.SelectedItems.Count > 0) { btnEditContent.Visibility = Visibility.Visible; }
            else { btnEditContent.Visibility = Visibility.Hidden; }

            // Update status message to show object IDs
            if (dataGridMain.SelectedItems.Count == 1) {
                textBoxStatus.Text = "RepositoryFileId: " + selectedContent.RepositoryFileId.ToString() +
                                     " | ActiveRevisionId: " + selectedContent.ActiveRevisionId.ToString();
            }

            if (dataGridMain.SelectedItems.Count > 1) { textBoxStatus.Text = "Multiple items selected. Select an individual row to review object ID's."; }
        }

        /// <summary>
        /// Edit button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnEditContent_Click(object sender, RoutedEventArgs e) {
            // Get selected item as a Content object
            if (!(dataGridMain.SelectedItem is Content selectedContent)) { return; }

            editContent(selectedContent);
        }

        private void DataGridMain_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (!(dataGridMain.SelectedItem is Content selectedContent)) { return; }

            editContent(selectedContent);
        }

        private void editContent(Content selectedContent) {
            // Show form for editing parameters
            gridEditParams.Visibility = Visibility.Visible;

            // Prepopulate Manufacturer and Model fields
            if (selectedContent.Manufacturer != "") { txtBxManufacturer.Text = selectedContent.Manufacturer; }

            if (selectedContent.Model != "") { txtBxModel.Text = selectedContent.Model; }

            // Get Revit Family Types
            selectedContent.FamilyTypes = Unifi.GetFamilyTypes(selectedContent);

            // Add each Revit Family Type to the combobox as items
            foreach (string familyType in selectedContent.FamilyTypes)
                comboFamilyTypes.Items.Add(familyType);

            // Select first Family Type in list
            comboFamilyTypes.SelectedIndex = 0;
        }

        /// <summary>
        /// Edit form Save button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_Click(object sender, RoutedEventArgs e) {
            // Get selected row as a Content object
            Content selectedItem = dataGridMain.SelectedItems.OfType<Content>().ToList()[0];

            // Get selected Type Name
            string familyTypeName = (comboFamilyTypes.SelectedValue).ToString();

            try {
                // Call API to set the Type Parameter value and retrieve the response as Batch object
                Batch batchManufacturer =
                    Unifi.SetTypeParameterValue(unifiToken, selectedItem, familyTypeName, "Manufacturer", txtBxManufacturer.Text, "TEXT", 2016);
                Batch batchModel = Unifi.SetTypeParameterValue(unifiToken, selectedItem, familyTypeName, "Model", txtBxModel.Text, "TEXT", 2016);

                comboBatches.Items.Add(batchManufacturer.BatchId);
                comboBatches.Items.Add(batchModel.BatchId);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }

            CloseEditForm();
        }

        /// <summary>
        /// Edit form Cancel button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { CloseEditForm(); }

        /// <summary>
        /// A method to call when closing the edit form modal window
        /// </summary>
        private void CloseEditForm() {
            // Clear data from form
            comboFamilyTypes.Items.Clear();
            txtBxManufacturer.Text = "";
            txtBxModel.Text = "";

            // Hide form for editing parameters
            gridEditParams.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Check the status of a batch and display monitor data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnRefreshBatchMon_Click(object sender, RoutedEventArgs e) {
            // Get BatchId from combobox
            if (comboBatches.SelectedItem == null) { return; }

            var batchId = comboBatches.SelectedItem.ToString();

            var loading = $"[{DateTime.Now.ToLocalTime()}] {batchId}: Loading...";

            listBatchStatus.Items.Add(loading);

            // Retrieve BatchStatus and display data
            var status = await Unifi.GetBatchStatus(unifiToken, batchId);

            listBatchStatus.Items.Remove(loading);
            listBatchStatus.Items.Add($"[{DateTime.Now.ToLocalTime()}] {batchId} {status.TotalFiles} Files | " +
                                      $"{status.PendingFiles} Pending | {status.OkFiles} Complete | {status.FailedFiles} Failed");
        }

        /// <summary>
        /// Show batch monitor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBatchMon_Click(object sender, RoutedEventArgs e) {
            // Set the combobox to select the most recent Batch
            comboBatches.SelectedIndex = comboBatches.Items.Count - 1;

            // Show the Batch Monitor
            gridBatchMonitor.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Close batch monitor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCloseBatchMon_Click(object sender, RoutedEventArgs e) { gridBatchMonitor.Visibility = Visibility.Hidden; }

        /// <summary>
        /// Clear batch monitor text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearBatchMon_Click(object sender, RoutedEventArgs e) { listBatchStatus.Items.Clear(); }
    }
}