﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vixen.Hardware;

namespace Vixen.Module.Output {
	public interface IOutputModuleInstance : IOutput, IControllerHardwareModule {
    }
}
