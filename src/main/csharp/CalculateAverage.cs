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

public static class Extensions
{

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

    // this is the most disgusting way I could think of to implement this
    public static Measurement operator +(Measurement value, float measurement)
    {
        value.Total += measurement;
        value.Min = Math.Min(value.Min, measurement);
        value.Max = Math.Max(value.Max, measurement);
        value.Count++;
        return value;
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



    // use Parallel.ForEach to parse the file and calculate the min, max, and average
    public static void CalculateParallel()
    {
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, Measurement>();

        Parallel.ForEach(lines,
           new ParallelOptions { MaxDegreeOfParallelism = 20 },
        line =>
        {
            // Split the line into station name and measurement
            var spa = line.AsSpan();
            var stationName = spa[..spa.IndexOf(';')].ToString();
            float measurement = float.Parse(spa[(spa.IndexOf(';') + 1)..]);


            if (stationMeasurements.TryGetValue(stationName, out Measurement value))
            {
                stationMeasurements[stationName] = value + measurement; // love operator overloading 
            }
            else
            {
                stationMeasurements.TryAdd(stationName, new Measurement(stationName, measurement));
            }
        });

        Console.Write("{");
        //var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();
        var measurements = new SortedDictionary<string, Measurement>(stationMeasurements);
        for (var i = 0; i < measurements.Count - 1; i++)
        {
            var station = measurements.ElementAt(i);
            Console.Write(station.Value);
            Console.Write(", ");
        }
        Console.Write(measurements.ElementAt(measurements.Count - 1).Value);
        Console.WriteLine("}");
    }


    public static async Task CalculateAsync()
    {
        var lines = File.ReadLinesAsync(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, List<float>>();

        await foreach (var line in lines)
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
        }

        var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();

        Console.Write("{");
        for (var i = 0; i < measurements.Length; i++)
        {
            var station = measurements.ElementAt(i);
            var (min, average, max) = SIMDMinMaxAverage2([.. station.Value]);
            Console.Write($"{station.Key}={min:0.0}/{average:0.0}/{max:0.0}");
            if (i < measurements.Length - 1)
            {
                Console.Write(", ");
            }
        }
        Console.WriteLine("}");
    }

    // use the SIMDMinMaxAverage2 method to calculate the min, max, and average faster (maybe?)
    public static void CalculateSimd()
    {
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, List<float>>();

        Parallel.ForEach(lines,
        new ParallelOptions { MaxDegreeOfParallelism = 10 },
         line =>
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


        // var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();
        var measurements = new SortedDictionary<string, List<float>>(stationMeasurements)
        .ToArray();
        Console.Write("{");
        for (var i = 0; i < measurements.Length; i++)
        {
            var station = measurements.ElementAt(i);
            var (min, average, max) = SIMDMinMaxAverage2([.. station.Value]);
            Console.Write($"{station.Key}={min:0.0}/{average:0.0}/{max:0.0}");
            if (i < measurements.Length - 1)
            {
                Console.Write(", ");
            }
        }
        Console.WriteLine("}");
    }

    // inline the SIMD code and stackalloc whatever we can
    public static void CalculateSimd2()
    {
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, List<float>>();

        Parallel.ForEach(lines,
        new ParallelOptions { MaxDegreeOfParallelism = 10 },
         line =>
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


        var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();

        Console.Write("{");
        Span<float> floats = stackalloc float[3];
        for (var k = 0; k < measurements.Length; k++)
        {
            var station = measurements.ElementAt(k);

            var simdLength = Vector<float>.Count;
            var vmin = new Vector<float>(float.MaxValue);
            var vmax = new Vector<float>(float.MinValue);
            var i = 0;
            float total = 0;

            // float min = float.MaxValue;
            // float max = float.MinValue;
            // float average = 0;

            var input = station.Value.ToArray();
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
                floats[0] = Math.Min(floats[0], vmin[j]);
                floats[2] = Math.Max(floats[2], vmax[j]);
            }
            for (; i < input.Length; ++i)
            {
                floats[0] = Math.Min(floats[0], input[i]);
                floats[2] = Math.Max(floats[2], input[i]);
                total += input[i];
            }

            floats[1] = total / input.Length;

            floats[0] = floats[0].Round();
            floats[2] = floats[2].Round();
            floats[1] = floats[1].Round();


            Console.Write($"{station.Key}={floats[0]:0.0}/{floats[1]:0.0}/{floats[2]:0.0}");
            if (i < measurements.Length - 1)
            {
                Console.Write(", ");
            }
        }
        Console.WriteLine("}");
    }

    // run the simd step in parallel
    public static void CalculateSimd3()
    {
        var lines = File.ReadLines(Path.GetFullPath(FILENAME));
        var stationMeasurements = new ConcurrentDictionary<string, List<float>>();

        Parallel.ForEach(lines,
        new ParallelOptions { MaxDegreeOfParallelism = 10 },
         line =>
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


        // var measurements = stationMeasurements.OrderBy(x => x.Key).ToArray();
        //var measurements = new SortedDictionary<string, List<float>>(stationMeasurements);
        var results = new ConcurrentDictionary<string, string>();
        //ConcurrentBag<string> results = new ConcurrentBag<string>();
        Parallel.ForEach(stationMeasurements,
         station =>
         {
             var (min, average, max) = SIMDMinMaxAverage2([.. station.Value]);
             results.TryAdd(station.Key, $"{station.Key}={min:0.0}/{average:0.0}/{max:0.0}");
         }
        );

        var measurements = new SortedDictionary<string, string>(results);
        Console.Write("{");
        for (var i = 0; i < results.Count - 1; i++)
        {
            var station = results.ElementAt(i);

            Console.Write(station.Value);

        }
        Console.Write(results.ElementAt(results.Count - 1).Value);
        Console.WriteLine("}");
    }


    // --------- HELPER METHODS ------------ //

    // based on: https://instil.co/blog/parallelism-on-a-single-core-simd-with-c/
    public static (float min, float mean, float max) SIMDMinMaxAverage2(float[] input)
    {
        var simdLength = Vector<float>.Count;
        var vmin = new Vector<float>(float.MaxValue);
        var vmax = new Vector<float>(float.MinValue);
        var i = 0;
        float total = 0;
        Span<float> floats = stackalloc float[3];
        float min = float.MaxValue;
        float max = float.MinValue;
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
}