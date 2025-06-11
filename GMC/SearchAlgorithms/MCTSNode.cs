using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC.SearchAlgorithms
{
    /// <summary>
    /// Represents a single node of the search tree built by MCTS.
    /// </summary>
    internal class MCTSNode : SearchNode
    {
        /// <summary>
        /// Determines whether all the actions that are possible in this node have been tried.
        /// </summary>
        public bool FullyExpanded { get; protected set; }

        public readonly MCTSNode parent;

        public List<MCTSNode> Children { get; protected set; }

        public MCTSNode(TreeNode state, bool team, MCTSNode parent) : base(state, team)
        {
            this.parent = parent;
            FullyExpanded = state.isTerminal;
            Children = new List<MCTSNode>();
        }

        /// <summary>
        /// Picks an action from the possible actions that haven't yet been tried and creates a child.
        /// </summary>
        public virtual MCTSNode Expand()
        {
            MCTSNode newNode = new MCTSNode(State.GetNextChild(), !team, this);
            Children.Add(newNode);

            if (Children.Count == State.Children.Count)
            {
                FullyExpanded = true;
            }

            return newNode;
        }

        /// <summary>
        /// Backpropagates the result of a playout.
        /// </summary>
        public virtual MCTSNode Backpropagate(double val)
        {
            double currentVal = val;
            MCTSNode currentNode = this;
            MCTSNode toReturn = null;

            while (currentNode != null)
            {
                // If the current node is the child of the root node, store it as
                // it needs to be returned.
                if (toReturn == null && currentNode.parent.parent == null)
                {
                    toReturn = currentNode;
                }

                currentNode.UpdateValue(currentVal);

                currentVal = 1 - currentVal;
                currentNode = currentNode.parent;
            }

            return toReturn;
        }
    }
}
