using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Globalization;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
public class welcomSceneHandler : MonoBehaviour
{
   
    string filepath_user;
    string filepath_session;
    public Image connectStatu;
    public TextMeshProUGUI statusText;
    public Image statusBackground;
    public GameObject loading;
    public TextMeshProUGUI name;
    public TextMeshProUGUI timeRemaining;
    public TextMeshProUGUI days;
   
    public int daysPassed;
    public DataTable dataTablesession;
    public DataTable dataTableConfig;
    public Image greenCircleImageDay1; // Partially filled green circle
    public Image greenCircleImageDay2; // Partially filled green circle
    public Image greenCircleImageDay3; // Partially filled green circle
    public Image greenCircleImageDay4; // Partially filled green circle
    public Image greenCircleImageDay5; // Partially filled green circle
    public Image greenCircleImageDay6; // Partially filled green circle
    public Image greenCircleImageDay7; // Partially filled green circle
    public Image[] greenCircleImages;
    DateTime startDate;
    // Total time (e.g., 90 minutes)
    public float totalTime = 90f;
    public bool piChartUpdated = false; 
    // Elapsed time (e.g., 30 minutes)
    public float[] elapsedTimeDay = new float[] { 0,0,0,0,0,0,0 };

    public float[] movTimeInMinutesArray;
    // Start is called before the first frame update
    void Start()
    {
        AppData.fileCreation.createFileStructure();
        filepath_user = AppData.fileCreation.filePath_UserData ;
        filepath_session = AppData.fileCreation.filePath_SessionData ;
        statusText.text = "connecting..";
        Pluto_SceneHandler.TryConnectToDevice();
      
    }

    // Update is called once per frame
    void Update()
    {
        
        
  
        if ((AppData.fileCreation.filePath_SessionData != null && !piChartUpdated)&& AppData.fileCreation.filePath_UserData!=null)
        {
            dataTableConfig = new DataTable();
            dataTablesession = new DataTable();
            LoadCSV(AppData.fileCreation.filePath_SessionData, dataTablesession);
            LoadCSV(AppData.fileCreation.filePath_UserData, dataTableConfig);
            CalculateMovTimePerDayWithLinq();
            updateUserData();
            UpdatePieChart();
        }
        
        
        if (ConnectToRobot.isPLUTO)
        {
            statusBackground.color = Color.green;
            statusText.text = "connected";
            connectStatu.color = Color.green;
            loading.SetActive(false);
            if (AppData.PlutoRobotData.buttonst == 0)
            {
                SceneManager.LoadScene("ChooseMechanism");
            }
        }
       
        
    }
    private void updateUserData()
    {
        if (dataTableConfig.Rows.Count > 0)
        {
            // Get the last row
            DataRow lastRow = dataTableConfig.Rows[dataTableConfig.Rows.Count - 1];
            // Access data from the last row, for example, using column names
            name.text = lastRow.Field<string>("name");
            timeRemaining.text = lastRow.Field<string>("time");
            String end = lastRow.Field<string>("end "); ;
            String start = lastRow.Field<string>("start");
     
            string dateFormat = "dd-MM-yyyy";

            // Parse the dates
             startDate = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);
            // Calculate the difference in days
            TimeSpan difference = endDate - startDate;
            days.text = calculateDaypassed()+"/"+ difference.Days.ToString();
        }
        else
        {
            Debug.Log("The DataTable is empty.");
        }

    }
    private void UpdatePieChart()
    {
        greenCircleImages = new Image[7]
       {
            greenCircleImageDay1, // Day 1
            greenCircleImageDay2, // Day 2
            greenCircleImageDay3, // Day 3
            greenCircleImageDay4, // Day 4
            greenCircleImageDay5, // Day 5
            greenCircleImageDay6, // Day 6
            greenCircleImageDay7  // Day 7
       };
        for (int i = 0; i < elapsedTimeDay.Length; i++)
        {
           
            // Calculate the percentage of elapsed time
            float elapsedPercentage = elapsedTimeDay[i] / totalTime;

            // Set the green circle's fill amount based on elapsed time
            greenCircleImages[i].fillAmount = elapsedPercentage;

            // Green color for the elapsed portion
            greenCircleImages[i].color = new Color32(148,234,107,255);
        }
        piChartUpdated = true;
    }
    private  void LoadCSV(string filePath,DataTable dataTable)
    {
        

        // Read all lines from the CSV file
        var lines = File.ReadAllLines(filePath);
        if (lines.Length == 0) return;

        // Read the header line to create columns
        var headers = lines[0].Split(','); // Assuming your columns are tab-separated
        foreach (var header in headers)
        {
            dataTable.Columns.Add(header);
        }

        // Read the rest of the data lines
        for (int i = 1; i < lines.Length; i++)
        {
            var row = dataTable.NewRow();
            var fields = lines[i].Split(',');
            for (int j = 0; j < headers.Length; j++)
            {
                row[j] = fields[j];
            }
            dataTable.Rows.Add(row);
        }

        //Debug.Log("CSV loaded into DataTable with " + dataTable.Rows.Count + " rows.");
    }
   
    public  void CalculateMovTimePerDayWithLinq()
    {
        // Step 1: Find the most recent date in the data, regardless of order
        DateTime maxDate = dataTablesession.AsEnumerable()
            .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
            .Max();

        // Step 2: Calculate the cutoff date (7 days before the most recent date)
        DateTime sevenDaysAgo = maxDate.AddDays(-7);

        // Step 3: Use LINQ to group by date, filter the last 7 days, calculate total movTime, and get the day of the week
        var movTimePerDay = dataTablesession.AsEnumerable()
             .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date >= sevenDaysAgo) // Filter the last 7 days
            .GroupBy(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date) // Group by date only
            .Select(group => new
            {
                Date = group.Key.ToString("yyyy-MM-dd"),      // Format date as "yyyy-MM-dd"
                DayOfWeek = group.Key.DayOfWeek.ToString(),   // Get the day of the week
                TotalMovTime = group.Sum(row => Convert.ToInt32(row["movTime"]))
            }).ToList();
        movTimeInMinutesArray = new float[movTimePerDay.Count];

        // Convert total movTime to minutes and store in the array
        for (int i = 0; i < movTimePerDay.Count; i++)
        {
            elapsedTimeDay[i] = movTimePerDay[i].TotalMovTime / 60f; // Convert seconds to minutes
        }
       
    }
    public int calculateDaypassed()
    {
        
        TimeSpan duration = DateTime.Now - startDate;
        daysPassed = (int)duration.TotalDays;
        return daysPassed;
    }
    private void OnApplicationQuit()
    {
        ConnectToRobot.disconnect();
    }
  
}
