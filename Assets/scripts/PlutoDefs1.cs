/*
 * Module containing definitions of different PLUTO-related variables.
 *
 * Author: Sivakumar Balasubramanian
 * Date: 25 July 2024
 * Email: siva82kb@gmail.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PlutoDefs1
{
    // Hand Opening and Closing Mechanism Conversion Factor
    public static readonly double HOCScale = 3.97 * Math.PI / 180;
    public static readonly double PLUTOMaxTorque = 1.0; // Nm

    public enum PlutoEvents
    {
        PRESSED = 0,
        RELEASED = 1,
        NEWDATA = 2
    }

    public static readonly Dictionary<string, byte> ControlType = new Dictionary<string, byte>
    {
        { "NONE", 0x00 },
        { "POSITION", 0x01 },
        { "RESIST", 0x02 },
        { "TORQUE", 0x03 }
    };

    public static readonly Dictionary<string, byte> Mechanisms = new Dictionary<string, byte>
    {
        { "WFE", 0x00 },
        { "WUD", 0x01 },
        { "WPS", 0x02 },
        { "HOC", 0x03 },
        { "NOMECH", 0x04 }
    };

    public static readonly Dictionary<string, byte> OutDataType = new Dictionary<string, byte>
    {
        { "SENSORSTREAM", 0x00 },
        { "CONTROLPARAM", 0x01 },
        { "DIAGNOSTICS", 0x02 }
    };

    public static readonly Dictionary<string, byte> InDataType = new Dictionary<string, byte>
    {
        { "GET_VERSION", 0x00 },
        { "CALIBRATE", 0x01 },
        { "START_STREAM", 0x02 },
        { "STOP_STREAM", 0x03 },
        { "SET_CONTROL_TYPE", 0x04 },
        { "SET_CONTROL_TARGET", 0x05 },
        { "SET_DIAGNOSTICS", 0x06 }
    };

    public static readonly Dictionary<string, byte> ControlDetails = new Dictionary<string, byte>
    {
        { "POSITIONTGT", 0x08 },
        { "FEEDFORWARDTGT", 0x20 }
    };

    public static readonly Dictionary<string, ushort> ErrorTypes = new Dictionary<string, ushort>
    {
        { "ANGSENSERR", 0x0001 },
        { "VELSENSERR", 0x0002 },
        { "TORQSENSERR", 0x0004 },
        { "MCURRSENSERR", 0x0008 }
    };
    public static readonly Dictionary<string, byte> ErrorTypesByte = ErrorTypes.ToDictionary(
       kvp => kvp.Key,
       kvp => (byte)kvp.Value
   );
    public static readonly Dictionary<string, byte> OperationStatus = new Dictionary<string, byte>
    {
        { "NOERR", 0x00 },
        { "YESERR", 0x01 }
    };

    public static readonly Dictionary<string, byte> CalibrationStatus = new Dictionary<string, byte>
    {
        { "NOCALIB", 0x00 },
        { "YESCALIB", 0x01 }
    };

    public static readonly Dictionary<string, int> PlutoAngleRanges = new Dictionary<string, int>
    {
        { "WFE", 120 },
        { "WUD", 120 },
        { "WPS", 120 },
        { "HOC", 140 }
    };

    public static readonly Dictionary<string, double[]> PlutoTargetRanges = new Dictionary<string, double[]>
    {
        { "TORQUE", new double[] { -PLUTOMaxTorque, PLUTOMaxTorque } },
        { "POSITION", new double[] { -135, 0 } }
    };

    public static readonly Dictionary<string, int> PlutoSensorDataNumber = new Dictionary<string, int>
    {
        { "SENSORSTREAM", 4 },
        { "DIAGNOSTICS", 7 }
    };

    public static string GetName(Dictionary<string, byte> defDict, byte code)
    {
        foreach (var kvp in defDict)
        {
            if (kvp.Value == code)
                return kvp.Key;
        }
        return null;
    }
    public static double[] GetTargetRange(string controlType)
    {
        if (PlutoTargetRanges.ContainsKey(controlType))
        {
            return PlutoTargetRanges[controlType];
        }
        throw new ArgumentException("Invalid control type");
    }
}
