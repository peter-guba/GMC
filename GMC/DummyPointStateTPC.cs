using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMC
{
    /// <summary>
    /// ITerminationProbabilityCalculator implementation for DummyPointState.
    /// </summary>
    internal class DummyPointStateTPC : ITerminationProbabilityCalculator
    {
        private bool startingTeam;

        private double hitProbability;

        private int whitePoints;

        private int blackPoints;

        private int currentDepth;

        // p stands for probability.

        private double pFinished;

        private double pWhiteFinished;

        private double pBlackFinished;

        public DummyPointStateTPC(DummyPointState origin)
        {
            startingTeam = origin.GetTeam();
            hitProbability = origin.GetHitProbability();
            whitePoints = origin.WhitePoints;
            blackPoints = origin.BlackPoints;
            Reset();
        }

        public void Reset()
        {
            currentDepth = 1;
            pFinished = 0.0;
            pWhiteFinished = 0.0;
            pBlackFinished = 0.0;
        }

        public double CalculateCurrentTerminationProbability()
        {
            double pB, pW;
            double result;

            // Compute the number of moves that each player has performed thus far.
            int wM = startingTeam ? (currentDepth + 1) / 2 : currentDepth / 2;
            int bM = !startingTeam ? (currentDepth + 1) / 2 : currentDepth / 2;

            // If the white player has had a chance to destroy all of black player's
            // points, compute the probability that it is so.
            if (wM >= blackPoints)
            {
                pW = Math.Pow(1 - hitProbability, wM - blackPoints) *
                Math.Pow(hitProbability, blackPoints) *
                AuxiliaryFunctions.BinomialCofficient(wM - 1, blackPoints - 1);
            }
            // Otherwise, the probability that the white player has won is 0.
            else
            {
                pW = 0;
            }

            // If the black player has had a chance to destroy all of white player's
            // points, compute the probability that it is so.
            if (bM >= whitePoints)
            {
                pB = Math.Pow(1 - hitProbability, bM - whitePoints) *
                Math.Pow(hitProbability, whitePoints) *
                AuxiliaryFunctions.BinomialCofficient(bM - 1, whitePoints - 1);
            }
            // Otherwise, the probability that the black player has won is 0.
            else
            {
                pB = 0;
            }
            
            // If the probability of the tree being finished has become too large for the double
            // type to represent it more precisely than 1, round the result to 1.
            if (pFinished == 1)
            {
                return 1;
            }

            // Calculate the conditional probability that the current player wins given that
            // the game hasn't ended yet. (P(A|B) = P(A and B)/P(B) which in this case is the
            // same as P(A)/P(B).
            if ((startingTeam && currentDepth % 2 == 1) ||
                (!startingTeam && currentDepth % 2 == 0))
            {
                result = pW * (1 - pBlackFinished) / (1 - pFinished);
                pWhiteFinished += pW;
                pFinished += pW * (1 - pBlackFinished);
            }
            else
            {
                result = (1 - pWhiteFinished) * pB / (1 - pFinished);
                pBlackFinished += pB;
                pFinished += (1 - pWhiteFinished) * pB;
            }

            ++currentDepth;
            return result;
        }
    }
}
