namespace DesktopLoudnessMeter.App.Audio.Analysis;

/// <summary>
/// Exact-value index over completed integrated-loudness block powers: a deterministic balanced (AVL)
/// binary search tree keyed by the unmodified measured power, where every node carries the number of
/// blocks that share its value and the sums needed to answer "sum and count of every block strictly
/// above this power" without walking the whole history.
/// </summary>
/// <remarks>
/// No level quantization, no bucketing and no history truncation happens here: the key is the exact
/// <see cref="double"/> the analyzer measured. Duplicate powers are counted on one node while their sum
/// is accumulated in arrival order, so a repeated value contributes exactly as often as it arrived.
/// Nodes are allocated once per distinct power and never removed until <see cref="Reset"/>; retention
/// therefore grows with the number of distinct powers in the session, which the metering notes state
/// explicitly instead of calling it bounded memory.
/// </remarks>
internal sealed class PowerAggregateIndex
{
    private Node? _root;

    /// <summary>
    /// Number of tree nodes the last <see cref="SumAbove"/> query touched. Test instrumentation only:
    /// production code never reads it. It is the observable that shows a completed gate block no longer
    /// rescans every retained block.
    /// </summary>
    internal int LastQueryNodeVisits { get; private set; }

    internal void Add(double power)
    {
        _root = Insert(_root, power);
    }

    /// <summary>
    /// Sums and counts every retained block whose power is strictly above <paramref name="threshold"/>,
    /// matching the original gating test <c>!(power &lt;= threshold)</c>.
    /// </summary>
    internal void SumAbove(double threshold, out double sum, out int count)
    {
        LastQueryNodeVisits = 0;
        if (double.IsNaN(threshold))
        {
            // A non-numeric preliminary mean makes every block pass the original comparison.
            LastQueryNodeVisits = _root is null ? 0 : 1;
            sum = _root?.SubtreeSum ?? 0;
            count = _root?.SubtreeCount ?? 0;
            return;
        }

        sum = 0;
        count = 0;
        CollectAbove(_root, threshold, ref sum, ref count);
    }

    internal void Reset()
    {
        _root = null;
        LastQueryNodeVisits = 0;
    }

    /// <summary>
    /// Total order for measured powers. "<c>!(power &lt;= threshold)</c>" is the gate's membership test, so
    /// a non-numeric power (which can only appear if a caller feeds one) sorts above every finite power
    /// and is gated exactly like the original scan gated it.
    /// </summary>
    private static int ComparePowers(double left, double right)
    {
        if (double.IsNaN(left))
        {
            return double.IsNaN(right) ? 0 : 1;
        }

        return double.IsNaN(right) ? -1 : left.CompareTo(right);
    }

    private void CollectAbove(Node? node, double threshold, ref double sum, ref int count)
    {
        if (node is null)
        {
            return;
        }

        LastQueryNodeVisits++;
        if (ComparePowers(node.Value, threshold) > 0)
        {
            // The node and its whole right subtree are above the threshold; the right subtree is
            // attached from its stored aggregate instead of being walked node by node.
            CollectAbove(node.Left, threshold, ref sum, ref count);
            sum += node.OwnSum;
            count += node.Multiplicity;
            if (node.Right is not null)
            {
                LastQueryNodeVisits++;
                sum += node.Right.SubtreeSum;
                count += node.Right.SubtreeCount;
            }

            return;
        }

        // The node and its whole left subtree are at or below the threshold.
        CollectAbove(node.Right, threshold, ref sum, ref count);
    }

    private Node Insert(Node? node, double power)
    {
        if (node is null)
        {
            return new Node(power);
        }

        var comparison = ComparePowers(power, node.Value);
        if (comparison == 0)
        {
            node.Multiplicity++;
            node.OwnSum += power;
            node.RefreshAggregates();
            return node;
        }

        if (comparison < 0)
        {
            node.Left = Insert(node.Left, power);
        }
        else
        {
            node.Right = Insert(node.Right, power);
        }

        node.RefreshAggregates();
        return Balance(node);
    }

    private static Node Balance(Node node)
    {
        var balance = HeightOf(node.Left) - HeightOf(node.Right);
        if (balance > 1)
        {
            if (HeightOf(node.Left!.Right) > HeightOf(node.Left.Left))
            {
                node.Left = RotateLeft(node.Left);
            }

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (HeightOf(node.Right!.Left) > HeightOf(node.Right.Right))
            {
                node.Right = RotateRight(node.Right);
            }

            return RotateLeft(node);
        }

        return node;
    }

    private static Node RotateLeft(Node node)
    {
        var pivot = node.Right!;
        node.Right = pivot.Left;
        pivot.Left = node;
        node.RefreshAggregates();
        pivot.RefreshAggregates();
        return pivot;
    }

    private static Node RotateRight(Node node)
    {
        var pivot = node.Left!;
        node.Left = pivot.Right;
        pivot.Right = node;
        node.RefreshAggregates();
        pivot.RefreshAggregates();
        return pivot;
    }

    private static int HeightOf(Node? node)
    {
        return node?.Height ?? 0;
    }

    private sealed class Node
    {
        public Node(double value)
        {
            Value = value;
            Multiplicity = 1;
            OwnSum = value;
            Height = 1;
            SubtreeCount = 1;
            SubtreeSum = value;
        }

        public double Value { get; }

        public int Multiplicity { get; set; }

        /// <summary>Sum of the duplicated powers stored on this node, accumulated in arrival order.</summary>
        public double OwnSum { get; set; }

        public int SubtreeCount { get; private set; }

        public double SubtreeSum { get; private set; }

        public int Height { get; private set; }

        public Node? Left { get; set; }

        public Node? Right { get; set; }

        public void RefreshAggregates()
        {
            var count = Multiplicity;
            var sum = OwnSum;
            if (Left is not null)
            {
                count += Left.SubtreeCount;
                sum += Left.SubtreeSum;
            }

            if (Right is not null)
            {
                count += Right.SubtreeCount;
                sum += Right.SubtreeSum;
            }

            SubtreeCount = count;
            SubtreeSum = sum;
            Height = 1 + Math.Max(HeightOf(Left), HeightOf(Right));
        }
    }
}
