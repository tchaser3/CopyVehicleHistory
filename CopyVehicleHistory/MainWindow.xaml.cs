/* Title:           Copy Vehicle History
 * Date:            6-21-17
 * Author:          Terry Holmes */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataValidationDLL;
using DateSearchDLL;
using NewEmployeeDLL;
using NewEventLogDLL;
using NewVehicleDLL;
using VehicleHistoryDLL;
using VehicleInYardDLL;

namespace CopyVehicleHistory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //setting up the classes
        WPFMessagesClass TheMessagesClass = new WPFMessagesClass();
        DataValidationClass TheDataValidationClass = new DataValidationClass();
        DateSearchClass TheDateSearchClass = new DateSearchClass();
        EmployeeClass TheEmployeeClass = new EmployeeClass();
        EventLogClass TheEventLogClass = new EventLogClass();
        VehicleClass TheVehicleClass = new VehicleClass();
        VehicleHistoryClass TheVehicleHistoryClass = new VehicleHistoryClass();
        VehicleInYardClass TheVehicleInYardClass = new VehicleInYardClass();

        OldVehicleHistoryDataSet aOldVehicleHistoryDataSet;
        OldVehicleHistoryDataSet TheOldVehicleHistoryDataSet;
        OldVehicleHistoryDataSetTableAdapters.vehiclehistoryTableAdapter aOldVehicleHistoryTableAdapter;

        OldVehicleInYardDataSet aOldVehicleInYardDataSet;
        OldVehicleInYardDataSet TheOldVehicleInYardDataSet;
        OldVehicleInYardDataSetTableAdapters.vehicleinyardTableAdapter aOldVehicleInYardTableAdapter;

        //setting up the sql data
        FindVehicleHistoryCompleteDataSet TheFindVehicleHistoryCompleteDataSet = new FindVehicleHistoryCompleteDataSet();
        VehicleHistoryDataSet TheVehicleHistoryDataSet;
        FindActiveVehicleByBJCNumberDataSet TheFindActiveVehicleByBJCNumberDataSet = new FindActiveVehicleByBJCNumberDataSet();
        FindVehiclesInYardVehicleIDDateMatchDataSet TheFindVehiclesInyardVehicleIDDateMatchDataSet = new FindVehiclesInYardVehicleIDDateMatchDataSet();

        int gintTransactionCounter;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            TheMessagesClass.CloseTheProgram();
        }
        private OldVehicleHistoryDataSet GetOldVehicleHistory()
        {
            try
            {
                aOldVehicleHistoryDataSet = new OldVehicleHistoryDataSet();
                aOldVehicleHistoryTableAdapter = new OldVehicleHistoryDataSetTableAdapters.vehiclehistoryTableAdapter();
                aOldVehicleHistoryTableAdapter.FindVehicleSignedOut(aOldVehicleHistoryDataSet.vehiclehistory);
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Copy Vehicle History // Get Old Vehicle History " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }

            return aOldVehicleHistoryDataSet;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this will load the old data set
            TheOldVehicleHistoryDataSet = GetOldVehicleHistory();

            TheVehicleHistoryDataSet = TheVehicleHistoryClass.GetVehicleHistoryInfo();

            gintTransactionCounter = 0;

            dgrVehicleHistory.ItemsSource = TheOldVehicleHistoryDataSet.vehiclehistory;
        }

        private void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            //setting local variables
            int intCounter;
            int intNumberOfRecords;
            int intVehicleID;
            int intEmployeeID;
            int intWarehouseEmployeeID;
            DateTime datTransactionDate;
            int intRecordReturned;
            bool blnFatalError = false;
            int intTranactionID;
            DateTime datLimitDate = DateTime.Now;

            PleaseWait PleaseWait = new PleaseWait();
            PleaseWait.Show();

            try
            {
                datLimitDate = TheDateSearchClass.SubtractingDays(datLimitDate, 30);

                intNumberOfRecords = TheOldVehicleHistoryDataSet.vehiclehistory.Rows.Count - 1;

                for(intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    intVehicleID = TheOldVehicleHistoryDataSet.vehiclehistory[intCounter].VehicleID;
                    intEmployeeID = TheOldVehicleHistoryDataSet.vehiclehistory[intCounter].EmployeeID;
                    intWarehouseEmployeeID = TheOldVehicleHistoryDataSet.vehiclehistory[intCounter].WarehouseEmployeeID;
                    datTransactionDate = TheOldVehicleHistoryDataSet.vehiclehistory[intCounter].Date;
                    intTranactionID = TheOldVehicleHistoryDataSet.vehiclehistory[intCounter].TransactionID;

                    if(datTransactionDate > datLimitDate)
                    {
                        TheFindVehicleHistoryCompleteDataSet = TheVehicleHistoryClass.FindVehicleHistoryComplete(intVehicleID, intEmployeeID, intWarehouseEmployeeID, datTransactionDate);

                        intRecordReturned = TheFindVehicleHistoryCompleteDataSet.FindVehicleHistoryComplete.Rows.Count;

                        if (intRecordReturned == 0)
                        {
                            VehicleHistoryDataSet.vehiclehistoryRow NewHistoryRow = TheVehicleHistoryDataSet.vehiclehistory.NewvehiclehistoryRow();

                            NewHistoryRow.VehicleID = intVehicleID;
                            NewHistoryRow.EmployeeID = intEmployeeID;
                            NewHistoryRow.WarehouseEmployeeID = intWarehouseEmployeeID;
                            NewHistoryRow.TransactionDate = datTransactionDate;
                            NewHistoryRow.TransactionID = gintTransactionCounter;

                            TheVehicleHistoryDataSet.vehiclehistory.Rows.Add(NewHistoryRow);
                            TheVehicleHistoryClass.UpdateVehicleHistoryDB(TheVehicleHistoryDataSet);
                            gintTransactionCounter--;
                        }
                    }
                }

                TheVehicleHistoryDataSet = TheVehicleHistoryClass.GetVehicleHistoryInfo();

                dgrVehicleHistory.ItemsSource = TheVehicleHistoryDataSet.vehiclehistory;

                LoadVehiclesInYard();
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Copy Vehicle History // Process Button " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }

            PleaseWait.Close();
        }
        private OldVehicleInYardDataSet GetOldVehicleInYardInfo()
        {
            try
            {
                aOldVehicleInYardDataSet = new OldVehicleInYardDataSet();
                aOldVehicleInYardTableAdapter = new OldVehicleInYardDataSetTableAdapters.vehicleinyardTableAdapter();
                aOldVehicleInYardTableAdapter.Fill(aOldVehicleInYardDataSet.vehicleinyard);
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Copy Vehicle History // Get Old Vehicle In Yard Info " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }

            return aOldVehicleInYardDataSet;
        }
        private void LoadVehiclesInYard()
        {
            int intCounter;
            int intNumberOfRecords;
            int intVehicleID;
            int intBJCNumber;
            int intRecordsReturned;
            DateTime datTransactionDate;
            bool blnFatalError = false;
            string strTestDate = "05/14/2016";
            DateTime datTestDate = Convert.ToDateTime(strTestDate);
            DateTime datLimitDate = DateTime.Now;

            try
            {
                TheOldVehicleInYardDataSet = GetOldVehicleInYardInfo();

                intNumberOfRecords = TheOldVehicleInYardDataSet.vehicleinyard.Rows.Count - 1;
                datLimitDate = TheDateSearchClass.SubtractingDays(datLimitDate, 30);

                for(intCounter = 0; intCounter <= intNumberOfRecords; intCounter++)
                {
                    intBJCNumber = TheOldVehicleInYardDataSet.vehicleinyard[intCounter].BJCNumber;
                    datTransactionDate = TheOldVehicleInYardDataSet.vehicleinyard[intCounter].Date;

                    if(datTransactionDate > datLimitDate)
                    {
                        TheFindActiveVehicleByBJCNumberDataSet = TheVehicleClass.FindActiveVehicleByBJCNumber(intBJCNumber);

                        intRecordsReturned = TheFindActiveVehicleByBJCNumberDataSet.FindActiveVehicleByBJCNumber.Rows.Count;

                        if (intRecordsReturned == 1)
                        {
                            intVehicleID = TheFindActiveVehicleByBJCNumberDataSet.FindActiveVehicleByBJCNumber[0].VehicleID;

                            TheFindVehiclesInyardVehicleIDDateMatchDataSet = TheVehicleInYardClass.FindVehiclesInYardVehicleIDDateMatch(intVehicleID, datTransactionDate);

                            intRecordsReturned = TheFindVehiclesInyardVehicleIDDateMatchDataSet.FindVehiclesInYardVehicleIDDateMatch.Rows.Count;

                            if (intRecordsReturned == 0)
                            {
                                blnFatalError = TheVehicleInYardClass.InsertVehicleInYard(datTransactionDate, intVehicleID);

                                if (blnFatalError == true)
                                {
                                    TheMessagesClass.ErrorMessage("There Is a Problem, Contact ID");
                                    return;
                                }
                            }

                        }
                    }

                    
                }
            }
            catch (Exception Ex)
            {
                TheEventLogClass.InsertEventLogEntry(DateTime.Now, "Copy Vehicle History // Load Vehicles In Yard " + Ex.Message);

                TheMessagesClass.ErrorMessage(Ex.ToString());
            }
        }
    }
}
