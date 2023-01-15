﻿using System;
using System.ComponentModel;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SysBot.Pokemon
{
    public class QueueSettings
    {
        private const string FeatureToggle = nameof(FeatureToggle);
        private const string UserBias = nameof(UserBias);
        private const string TimeBias = nameof(TimeBias);
        private const string QueueToggle = nameof(QueueToggle);
        public override string ToString() => "队列加入设置Queue Joining Settings";

        // General

        [Category(FeatureToggle), Description("切换用户是否可以加入队列\nToggles if users can join the queue.")]
        public bool CanQueue { get; set; } = true;

        [Category(FeatureToggle), Description("如果队列中已经有该数量的用户，则禁止添加用户\nPrevents adding users if there are this many users in the queue already.")]
        public int MaxQueueCount { get; set; } = 999;

        [Category(FeatureToggle), Description("允许用户在交易时取消排队\nAllows users to dequeue while being traded.")]
        public bool CanDequeueIfProcessing { get; set; }

        [Category(FeatureToggle), Description("确定Flex模式将如何处理队列\nDetermines how Flex Mode will process the queues.")]
        public FlexYieldMode FlexMode { get; set; } = FlexYieldMode.Weighted;

        [Category(FeatureToggle), Description("确定队列何时被打开和关闭\nDetermines when the queue is turned on and off.")]
        public QueueOpening QueueToggleMode { get; set; } = QueueOpening.Threshold;

        [Category(FeatureToggle), Description("是否打开批量文件夹功能")]
        public bool MutiTrade { get; set; } = true;


        // Queue Toggle

        [Category(QueueToggle), Description("Threshold模式:将导致队列打开的用户数\nThreshold Mode: Count of users that will cause the queue to open.")]
        public int ThresholdUnlock { get; set; } = 0;

        [Category(QueueToggle), Description("Threshold模式:将导致队列关闭的用户数\nThreshold Mode: Count of users that will cause the queue to close.")]
        public int ThresholdLock { get; set; } = 30;

        [Category(QueueToggle), Description("Scheduled模式:在队列锁定之前，被打开的秒数\nScheduled Mode: Seconds of being open before the queue locks.")]
        public int IntervalOpenFor { get; set; } = 5 * 60;

        [Category(QueueToggle), Description("Scheduled模式:在队列解锁前被关闭的秒数\nScheduled Mode: Seconds of being closed before the queue unlocks.")]
        public int IntervalCloseFor { get; set; } = 15 * 60;

        // Flex Users

        [Category(UserBias), Description("根据队列中的用户数量，对交易队列的权重进行倾斜\nBiases the Trade Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountTrade { get; set; } = 100;

        [Category(UserBias), Description("根据队列中的用户数量，对种子检查Seed Check队列的权重进行倾斜\nBiases the Seed Check Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountSeedCheck { get; set; } = 100;

        [Category(UserBias), Description("根据队列中的用户数量，对克隆队Clone列的权重进行偏置\nBiases the Clone Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountClone { get; set; } = 100;

        [Category(UserBias), Description("根据队列中的用户数量，偏重于Dump队列的权重\nBiases the Dump Queue's weight based on how many users are in the queue.")]
        public int YieldMultCountDump { get; set; } = 100;

        [Category(UserBias), Description("让第几个用户提前准备")]
        public int AlertNumber { get; set; } = 3;

        // Flex Time

        [Category(TimeBias), Description("决定是否应将重量加入或乘以总重量\nDetermines whether the weight should be added or multiplied to the total weight.")]
        public FlexBiasMode YieldMultWait { get; set; } = FlexBiasMode.Multiply;

        [Category(TimeBias), Description("检查用户加入Trade队列后经过的时间，并相应增加队列的权重\nChecks time elapsed since the user joined the Trade queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitTrade { get; set; } = 1;

        [Category(TimeBias), Description("检查用户加入Seed Check队列后经过的时间，并相应增加队列的权重\nChecks time elapsed since the user joined the Seed Check queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitSeedCheck { get; set; } = 1;

        [Category(TimeBias), Description("检查用户加入Clone队列后经过的时间，并相应增加队列的权重\nChecks time elapsed since the user joined the Clone queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitClone { get; set; } = 1;

        [Category(TimeBias), Description("检查用户加入Dump队列后经过的时间，并相应增加队列的权重\nChecks time elapsed since the user joined the Dump queue, and increases the queue's weight accordingly.")]
        public int YieldMultWaitDump { get; set; } = 1;

        [Category(TimeBias), Description("将队列中的用户数量相乘，以估计到用户被处理需要多少时间\nMultiplies the amount of users in queue to give an estimate of how much time it will take until the user is processed.")]
        public float EstimatedDelayFactor { get; set; } = 1.1f;

        private int GetCountBias(PokeTradeType type) => type switch
        {
            PokeTradeType.Seed => YieldMultCountSeedCheck,
            PokeTradeType.Clone => YieldMultCountClone,
            PokeTradeType.Dump => YieldMultCountDump,
            _ => YieldMultCountTrade,
        };

        private int GetTimeBias(PokeTradeType type) => type switch
        {
            PokeTradeType.Seed => YieldMultWaitSeedCheck,
            PokeTradeType.Clone => YieldMultWaitClone,
            PokeTradeType.Dump => YieldMultWaitDump,
            _ => YieldMultWaitTrade,
        };

        /// <summary>
        /// Gets the weight of a <see cref="PokeTradeType"/> based on the count of users in the queue and time users have waited.
        /// </summary>
        /// <param name="count">Count of users for <see cref="type"/></param>
        /// <param name="time">Next-to-be-processed user's time joining the queue</param>
        /// <param name="type">Queue type</param>
        /// <returns>Effective weight for the trade type.</returns>
        public long GetWeight(int count, DateTime time, PokeTradeType type)
        {
            var now = DateTime.Now;
            var seconds = (now - time).Seconds;

            var cb = GetCountBias(type) * count;
            var tb = GetTimeBias(type) * seconds;

            return YieldMultWait switch
            {
                FlexBiasMode.Multiply => cb * tb,
                _ => cb + tb,
            };
        }

        /// <summary>
        /// Estimates the amount of time (minutes) until the user will be processed.
        /// </summary>
        /// <param name="position">Position in the queue</param>
        /// <param name="botct">Amount of bots processing requests</param>
        /// <returns>Estimated time in Minutes</returns>
        public float EstimateDelay(int position, int botct) => (EstimatedDelayFactor * position) / botct;
    }

    public enum FlexBiasMode
    {
        Add,
        Multiply,
    }

    public enum FlexYieldMode
    {
        LessCheatyFirst,
        Weighted,
    }

    public enum QueueOpening
    {
        Manual,
        Threshold,
        Interval,
    }
}