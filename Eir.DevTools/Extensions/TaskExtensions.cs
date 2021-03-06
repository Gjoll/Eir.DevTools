﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Eir.DevTools
{
    public static class TaskExtensions
    {
        public static T WaitResult<T>(this Task<T> task)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));
            task.Wait();
            return task.Result;
        }
    }
}
