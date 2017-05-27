﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.EventHubs.Tests.Client
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class MiscTests : ClientTestBase
    {
        [Fact]
        [DisplayTestMethodName]
        async Task PartitionKeyValidation()
        {
            int NumberOfMessagesToSend = 100;
            var partitionOffsets = new Dictionary<string, string>();

            // Discover the end of stream on each partition.
            TestUtility.Log("Discovering end of stream on each partition.");
            foreach (var partitionId in this.PartitionIds)
            {
                var lastEvent = await DiscoverEndOfStreamForPartition(partitionId);
                partitionOffsets.Add(partitionId, lastEvent.Item1);
                TestUtility.Log($"Partition {partitionId} has last message with offset {lastEvent.Item1}");
            }

            // Now send a set of messages with different partition keys.
            TestUtility.Log($"Sending {NumberOfMessagesToSend} messages.");
            Random rnd = new Random();
            for (int i = 0; i < NumberOfMessagesToSend; i++)
            {
                var partitionKey = rnd.Next(10);
                await this.EventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes("Hello EventHub!")), partitionKey.ToString());
            }

            // It is time to receive all messages that we just sent.
            // Prepare partition key to partition map while receiving.
            // Validation: All messages of a partition key should be received from a single partition.
            TestUtility.Log("Starting to receive all messages from each partition.");
            var partitionMap = new Dictionary<string, string>();
            int totalReceived = 0;
            foreach (var partitionId in this.PartitionIds)
            {
                PartitionReceiver receiver = null;
                try
                {
                    receiver = this.EventHubClient.CreateReceiver(
                        PartitionReceiver.DefaultConsumerGroupName,
                        partitionId,
                        partitionOffsets[partitionId]);
                    var messagesFromPartition = await ReceiveAllMessages(receiver);
                    TestUtility.Log($"Received {messagesFromPartition.Count} messages from partition {partitionId}.");
                    foreach (var ed in messagesFromPartition)
                    {
                        var pk = ed.SystemProperties.PartitionKey;
                        if (partitionMap.ContainsKey(pk) && partitionMap[pk] != partitionId)
                        {
                            throw new Exception($"Received a message from partition {partitionId} with partition key {pk}, whereas the same key was observed on partition {partitionMap[pk]} before.");
                        }

                        partitionMap[pk] = partitionId;
                    }

                    totalReceived += messagesFromPartition.Count;
                }
                finally
                {
                    await receiver.CloseAsync();
                }
            }

            Assert.True(totalReceived == NumberOfMessagesToSend,
                $"Didn't receive the same number of messages that we sent. Sent: {NumberOfMessagesToSend}, Received: {totalReceived}");
        }
    }
}