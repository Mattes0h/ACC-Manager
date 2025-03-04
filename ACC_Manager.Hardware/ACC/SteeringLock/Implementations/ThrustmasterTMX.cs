﻿using System;

namespace ACCManager.Hardware.ACC.SteeringLock.Implementations
{
    internal class ThrustmasterTMX : ThrustmasterT500 {
        public override string ControllerName => "Thrustmaster TMX";

        public override bool Test(string productGuid) {
            return string.Equals(productGuid, "B67F044F-0000-0000-0000-504944564944", StringComparison.OrdinalIgnoreCase);
        }

        protected override int ProductId => 0xb67f;
    }
}