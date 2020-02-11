﻿using System;
using EventStore.Core.Messages;
using NUnit.Framework;

namespace EventStore.Core.Tests.Services.Replication.ReplicationTracking {

	[TestFixture]
	public class when_5_node_cluster_has_2_lagging_nodes : with_clustered_replication_tracking_service {
		private readonly long _firstLogPosition = 2000;
		private readonly long _secondLogPosition = 4000;
		private Guid[] _slaves;

		protected override int ClusterSize => 5;
		
		public override void When() {
			_slaves = new [] {Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()};
			BecomeLeader();
			// All of the nodes have acked the first write
			WriterCheckpoint.Write(_firstLogPosition);
			WriterCheckpoint.Flush();
			Service.Handle(new ReplicationTrackingMessage.WriterCheckpointFlushed());
			foreach (var slave in _slaves) {
				Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(slave, _firstLogPosition));
			}
			AssertEx.IsOrBecomesTrue(() => Service.IsCurrent());

			ReplicatedTos.Clear();
			
			// Slaves 3 and 4 are lagging behind, they ack the previous positions
			WriterCheckpoint.Write(_secondLogPosition);
			WriterCheckpoint.Flush();
			Service.Handle(new ReplicationTrackingMessage.WriterCheckpointFlushed());
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slaves[0], _secondLogPosition));
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slaves[1], _secondLogPosition));
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slaves[2], _firstLogPosition));
			Service.Handle(new ReplicationTrackingMessage.ReplicaWriteAck(_slaves[3], _firstLogPosition));
			AssertEx.IsOrBecomesTrue(() => Service.IsCurrent());
		}

		[Test]
		public void replicated_to_should_be_sent_for_the_second_position() {
			Assert.True(ReplicatedTos.TryDequeue(out var msg));
			Assert.AreEqual(_secondLogPosition, msg.LogPosition);
		}

		[Test]
		public void replication_checkpoint_should_advance() {
			Assert.AreEqual(_secondLogPosition, ReplicationCheckpoint.Read());
			Assert.AreEqual(_secondLogPosition, ReplicationCheckpoint.ReadNonFlushed());
		}
	}
}
