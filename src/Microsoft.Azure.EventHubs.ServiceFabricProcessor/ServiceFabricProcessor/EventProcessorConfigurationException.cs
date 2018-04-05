﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.using System;

using System;

namespace Microsoft.Azure.EventHubs.ServiceFabricProcessor
{
    public class EventProcessorConfigurationException : Exception
    {
        public EventProcessorConfigurationException(string message) : base(message)
        {
        }
    }
}