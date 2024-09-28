using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class SessionDataHandler
{
    private DataTable sessionTable;
    private string filePath;

    public SessionDataHandler(string path)
    {
        filePath = path;
        LoadSessionData();
    }

    // Loads CSV data into a DataTable
    private void LoadSessionData()
    {
        sessionTable = new DataTable();
        if (File.Exists(filePath))
        {
            // Read all lines from the CSV file
            var lines = File.ReadAllLines(filePath);

            // Get column names from the first line
            string[] headers = lines[0].Split(',');
            foreach (var header in headers)
            {
                sessionTable.Columns.Add(header.Trim());
            }

            // Read and add each row to the DataTable
            for (int i = 1; i < lines.Length; i++)
            {
                string[] rowData = lines[i].Split(',');
                sessionTable.Rows.Add(rowData);
            }
        }
        else
        {
           UnityEngine.Debug.Log("CSV file not found at: " + filePath);
        }
    }

    // Method to calculate total time for each mechanism based on the current date
    public Dictionary<string, double> CalculateTotalTimeForMechanisms(DateTime currentDate)
    {
        // Dictionary to store total minutes for each mechanism
        var mechanismTime = new Dictionary<string, double>();

        // Filter rows based on the provided date
        var rowsForCurrentDate = sessionTable.AsEnumerable()
            .Where(row => DateTime.TryParse(row["DateTime"].ToString(), out DateTime rowDate) && rowDate.Date == currentDate.Date);

        foreach (DataRow row in rowsForCurrentDate)
        {
            // Read StartTime, StopTime, and Mechanism
            string startTimeStr = row["StartTime"].ToString();
            string stopTimeStr = row["StopTime"].ToString();
            string mechanism = row["mech"].ToString();

            // Calculate the difference between StartTime and StopTime
            if (DateTime.TryParse(startTimeStr, out DateTime startTime) && DateTime.TryParse(stopTimeStr, out DateTime stopTime))
            {
                double sessionMinutes = (stopTime - startTime).TotalMinutes;

                // Add sessionMinutes to the corresponding mechanism in the dictionary
                if (mechanismTime.ContainsKey(mechanism))
                {
                    mechanismTime[mechanism] += sessionMinutes;
                }
                else
                {
                    mechanismTime[mechanism] = sessionMinutes;
                }
            }
        }

        return mechanismTime;  // Return the dictionary containing total time per mechanism
    }
}
