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

public static class Extensions {

    public static float Round(this float f) => float.Round(f, 1);
}

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
    //const string FILENAME = "measurements.txt";
    const string FILENAME = "measurements.txt";

   // based on: https://instil.co/blog/parallelism-on-a-single-core-simd-with-c/
    public static (float min, float mean, float max) SIMDMinMaxAverage2(float[] input)
    {
        var simdLength = Vector<float>.Count;
        var vmin = new Vector<float>(float.MaxValue);
        var vmax = new Vector<float>(float.MinValue);
        var i = 0;
        float total = 0;
        float min = float.MaxValue;
        float max =  float.MinValue;
        float average = 0;

        // Need to multiply by 2 or we go out of bounds
        var lastSafeVectorIndex = input.Length - simdLength * 2;
  

        for (i = 0; i <= lastSafeVectorIndex; i += simdLength)
        {
            total = total + input[i] + input[i + 1] + input[i + 2] + input[i + 3];
            total = total + input[i + 4] + input[i + 5] + input[i + 6] + input[i + 7];
            var vector = new Vector<float>(input, i);
            vmin = Vector.Min(vector, vmin);
            vmax = Vector.Max(vector, vmax);
        }

        for (var j = 0; j < simdLength; ++j)
        {
            min = Math.Min(min, vmin[j]);
            max = Math.Max(max, vmax[j]);
        }
        for (; i < input.Length; ++i)
        {
            min = Math.Min(min, input[i]);
            max = Math.Max(max, input[i]);
            total += input[i];
        }

        average = total / input.Length;

        min = min.Round();
        max = max.Round();
        average = average.Round();

        return (min, average, max);
    }
  
    public static void CalculateSimd()
    {
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, List<float>>();

        Parallel.ForEach(lines, line =>
        {
            // Split the line into station name and measurement
            var parts = line.Split(";");
            var stationName = parts[0];
            var measurement = float.Parse(parts[1]);


            if (stationMeasurements.TryGetValue(stationName, out var value))
            {
                value.Add(measurement);
            }
            else
            {
                stationMeasurements.TryAdd(stationName, [measurement]);
            }
        });

        Console.Write("{");
        var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();
        for (var i = 0; i < measurements.Length; i++)
        {
            var station = measurements.ElementAt(i);
            //SIMDMinMaxAverage(station.Value.ToArray(), out var min, out var max, out var average);
            var (min, average, max) = SIMDMinMaxAverage2([.. station.Value]);
            Console.Write($"{station.Key}={min:0.0}/{average:0.0}/{max:0.0}");
            if (i < stationMeasurements.Count - 1)
            {
                Console.Write(", ");
            }
        }
        Console.WriteLine("}");
    }

    public static void CalculateFast()
    {
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, Measurement>();

        Parallel.ForEach(lines, line =>
        {
            // Split the line into station name and measurement
            var parts = line.Split(";");
            var stationName = parts[0];
            var measurement = float.Parse(parts[1]);


            if (stationMeasurements.TryGetValue(stationName, out var value))
            {
                value.Total += measurement;
                value.Min = Math.Min(value.Min, measurement);
                value.Max = Math.Max(value.Max, measurement);
                value.Count++;
                stationMeasurements[stationName] = value;
            }
            else
            {
                stationMeasurements.TryAdd(stationName, new Measurement(stationName, measurement));
            }
        });

        Console.Write("{");
        var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();
        for (var i = 0; i < measurements.Length; i++)
        {
            var station = measurements.ElementAt(i);
            Console.Write(station.Value);
            if (i < stationMeasurements.Count - 1)
            {
                Console.Write(", ");
            }
        }
        Console.WriteLine("}");
    }



    public static void CalculateBaseline()
    {
        // read everyting into memory
        var lines = File.ReadAllLines(FILENAME);
        Dictionary<string, List<double>> stationMeasurements = new Dictionary<string, List<double>>();

        foreach (string line in lines)
        {
            // Split the line into station name and measurement
            var parts = line.Split(';');
            string stationName = parts[0];
            double measurement = double.Parse(parts[1]);

            if (stationMeasurements.TryGetValue(stationName, out List<double>? value))
            {
                value.Add(measurement);
            }
            else
            {
                List<double> measurements = [measurement];
                stationMeasurements.Add(stationName, measurements);
            }
        }

        Console.Write("{");
        for (var i = 0; i < stationMeasurements.Count; i++)
        {
            var station = stationMeasurements.ElementAt(i);
            double min = station.Value.Min();
            double mean = station.Value.Average();
            double max = station.Value.Max();
            Console.WriteLine($"{station.Key}={min}/{mean}/{max}");
            if (i < stationMeasurements.Count - 1)
            {
                Console.Write(", ");
            }
        }
        Console.WriteLine("}");
    }
}