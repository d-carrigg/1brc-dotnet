/*
 *  Copyright 2023 The original authors
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

namespace BillionRows;

using System.Numerics;
using System.Collections.Concurrent;

internal struct Measurement
{
    public string Key { get; set; }
    public float Min { get; set; }
    public float Max { get; set; }
    public float Total { get; set; }
    public int Count { get; set; }

    public Measurement(string key, float value)
    {
        Key = key;
        Min = value;
        Max = value;
        Total = value;
        Count = 1;
    }

    public Measurement(string key, float min, float max, float total, int count)
    {
        Key = key;
        Min = min;
        Max = max;
        Total = total;
        Count = count;
    }

    // extracted in case I need to change it later
    private float Round(float f) => float.Round(f, 1);

    public (float min, float mean, float max) Summarize()
    {
        return (Round(Min), Round(Total / Count), Round(Max));

    }

    public override string ToString()
    {
        return $"{Key}={Round(Min):0.0}/{Round(Total / Count):0.0}/{Round(Max):0.0}";
    }
}

public sealed class CalcualteAverage
{
    const string FILENAME = "measurements.txt";
 
    public static void CalculateFast()
    {
        // var time = DateTime.Now;
        // read the file
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        // Console.WriteLine($"Read file in {(DateTime.Now - time).TotalMilliseconds} ms");
        // time = DateTime.Now;

        // create a dictionary to store the station name and the list of measurements
        var stationMeasurements = new ConcurrentDictionary<string, Measurement>();

        Parallel.ForEach(lines, line =>
        {
            // Split the line into station name and measurement
            var parts = line.Split(";");
            var stationName = parts[0];
            var measurement = float.Parse(parts[1]);

            // Check if the station name is already in the dictionary
            if (stationMeasurements.TryGetValue(stationName, out var value))
            {
                value.Total += measurement;
                value.Min = Math.Min(value.Min, measurement);
                value.Max = Math.Max(value.Max, measurement);

                // calculate running average
                value.Count++;

                stationMeasurements[stationName] = value;
            }
            else
            {
                stationMeasurements.TryAdd(stationName, new Measurement(stationName, measurement));
            }
        });

        //Console.WriteLine($"Parsed Stations in {(DateTime.Now - time).TotalMilliseconds} ms");
        // time = DateTime.Now;

        Console.Write("{");
        // Loop through the dictionary
        var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();
        for (var i = 0; i < stationMeasurements.Count; i++)
        {
            var station = measurements.ElementAt(i);
            // Calculate the min, mean, and max temperature


            // Print the result
            Console.Write(station.Value);

            if (i < stationMeasurements.Count - 1)
            {
                Console.Write(", ");
            }
        }

        Console.WriteLine("}");
        //Console.WriteLine($"Calculated Averages and printed in {(DateTime.Now - time).TotalMilliseconds} ms");
    }



    public void CalculateBaseline()
    {
        // Read the file
        var lines = File.ReadAllLines(FILENAME);


        // Create a dictionary to store the station name and the list of measurements
        Dictionary<string, List<double>> stationMeasurements = new Dictionary<string, List<double>>();

        // Loop through the lines
        foreach (string line in lines)
        {
            // Split the line into station name and measurement
            var parts = line.Split(';');
            string stationName = parts[0];
            double measurement = double.Parse(parts[1]);

            // Check if the station name is already in the dictionary
            if (stationMeasurements.ContainsKey(stationName))
            {
                // Add the measurement to the list
                stationMeasurements[stationName].Add(measurement);
            }
            else
            {
                // Create a new list with the measurement and add it to the dictionary
                List<double> measurements = new List<double>();
                measurements.Add(measurement);
                stationMeasurements.Add(stationName, measurements);
            }
        }

        Console.Write("{");
        // Loop through the dictionary
        foreach (KeyValuePair<string, List<double>> station in stationMeasurements)
        {
            // Calculate the min, mean, and max temperature
            double min = station.Value.Min();
            double mean = station.Value.Average();
            double max = station.Value.Max();

            // Print the result
            Console.WriteLine($"{station.Key}: min = {min}, mean = {mean}, max = {max}");
        }
    }
}