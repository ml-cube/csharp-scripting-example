// This file is not compiled as part of the project, it's
// copied as content to the output folder.

using System;

namespace CSharpScriptExample
{
    public static class Model2
    {
        public static int Predict(int maxValue)
        {
            return new Random().Next(maxValue);
        }
    }
}
