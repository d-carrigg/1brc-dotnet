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

public class Program
{

    public static void Main(string[] args)
    {

        // if the first arg passed in matches SIMD, PARALLEL, BASELINE
        // then run the corresponding test
        if (args.Length > 0)
        {
             Console.WriteLine($"Running {args[0]} test");
            switch (args[0].ToUpper())
            {
                case "SIMD":
                   
                    CalcualteAverage.CalculateSimd();
                    return;
                case "PARALLEL":
                    CalcualteAverage.CalculateParallel();
                    return;
                case "BASELINE":
                    CalcualteAverage.CalculateBaseline();
                    return;
                default:
                    Console.WriteLine("Unknown argument: " + args[0]);
                    return;
            }
        }
        else
        {
            // run tests
            CalcualteAverage.CalculateBaseline();
        }


    }
}


