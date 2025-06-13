# GMC toolkit

This is a C# command line application for generating random game trees and running MCTS over them. This README contains a description of the projects main methods and capabilities. The parameters of the specific trees we used in our experiments can be found in the tree_parameters file.



## Table of Contents
- [Software requirements](#software-requirements)
- [Main project commands](#main-project-commands)
- [Implemented algorithms](#implemented-algorithms)
- [Data visualizer script](#data-visualizer-script)
- [File formats](#file-formats)
- [References](#references)



## Software requirements
The project was created in Visual Studio 2022 version 17.8.5 targeting .NET 6 and some version of this software is necessary to open it. Once you have it open, you can issue commands by changing the command line arguments in the project's debug properties, or compile it and run the program from the command line.



## Main project commands

The main project offers the following commands:

**--check**, **-h** [path to tree file]\
	Checks if a given tree has been generated correctly (i.e. that all the children positions specified in the file lead to valid positions).


**--crunch**, **-c** [--pattern/-p, best choice flag present, regex, input path, output path] / [best choice flag present, number of iterations, tree file name, input path, output path]\
	Computes the mean and variance of data from files that match a given regex. If the --pattern option isn't specified, the regex is constructed to fit a tree with the given name and given number of iterations.\
	- The best choice flag present parameter is a boolean that determines whether the supplied files only contain data about convergence, or also about the performance (an explanation of the structure of output files and the data contain within them can be found in section X).\
	- The input path is a path leading to the folder where all possible input files are stored (the program doesn't go into subfolders).\
	- The output path is a path leading to the output file, including its name.


**--deceptionCheck**, **-d** [file to check]\
	Checks if the tree stored in the given file is deceptive, i.e. the node with the highest win ratio doesn't have the highest minimax value. The program assumes that more than one child is necessary for this, so it traverses the tree from the root until it finds a node with more than one child.


**--estimate**, **-e** [maximum branching factor, first player's points, second player's points, hit probability, maximum depth, number of samples]\
	!!!THIS COMMAND ISN'T FULLY DEBUGGED YET AND SOMETIMES RETURNS WRONG ESTIMATES!!!\
	Computes estimates of the mean size of trees generated using the given parameters, their standard deviation and 95% confidence interval. All parameters except "number of samples" directly correspond to parameters supplied to the generate command. The seed parameter is left out as this command is supposed to estimate the mean size and standard deviation for trees generated using all possible random seeds.\
	- "Number of samples" is used when computing the standard deviation, as, unlike the mean, this is computed by estimating the size of a number of trees and computing the standard deviation from that. The number of trees to be taken into account is specified by this parameter.


**--generate**, **-g** [maximum branching factor, maximum depth, first player's points, second player's points, hit probability, seed, output path]\
	Randomly generates a tree for a two-player zero-sum game using the given parameters. The generation starts with a root node. The current state of the game is given by the number of points each player has - these are initialised to the values passed through the parameters. Until all the nodes are considered terminal, the program generates a random number of children with the upper bound being the maximum branching factor at the start, but progressively changes to 3 at the last level. For every child, the score of the opponent is decreased with a probability given by the hit probability parameter. If a player reaches zero points, the game is over and the corresponding node is considered terminal, with its minimax score equal to 1 if the starting player won and 0 otherwise. If the maximum depth is reached before then, the node at this depth is also considered terminal and assigned a minimax value of 0.5. Every random value used during the construction of the tree is generated using a standard C# System.Random generator, which gets the seed specified by the user as its parameter. The output path has to contain the name of the output file.


**--help** []\
    Display a help message.


**--measure**, **-m** [algorithm name, path to tree file, number of iterations, per node, number of repeats, reward type, best only, more than one child necessary, output path]\
	Runs a given algorithm on the tree specified in the given file for a given number of iterations and repeats the process a given number of times. The result of every repetition is one or more files that contain data about the algorithm's convergence and performance at every iteration. An explanation of the file's format and containing data can be found in the section on file formats.\
	- The names of currently implemented algorithms are mentioned in section [Implemented algorithms] (#implemented-algorithms), specifically the "Command name" field under each algorithm.
	- The "per node" parameter is of type boolean and determines whether the specified number of iterations should be applied to every node, or whether it's the number of iterations for the whole algorithm.\
	- "Reward type" specifies the type of reward to which convergence should be measured - currently specified types are minimax values (mm) and win ratio (wr). The latter is the probability that making random moves will lead one to a winning state.\
	- The "best only" parameter is of type boolean and specifies whether data should be gathered for all children of the root node or just for the best one. If set to true, it also means that, besides data on convergence rate, data on whether the best node would be chosen (the node with the highest minimax value and, if there are more such nodes, the one among them that has the highest win ratio) and whether one of the nodes with the highest minimax value would be chosen.\
	- "More than one child necessary" is a boolean that specifies whether the starting node needs to have more than one child. When measuring the performance of MCTS, this is necessary, since the algorithm would otherwise always pick the best node, but the framework can also be used to measure the convergence rate of making random simulations to the win ratio of a node, which doesn't necessitate more than one child. If the parameter is set to true, the algorithm traverses the tree from the root until it find the first node that has more than one child and uses that as the root of the search.\
	- The "output path" is specified without the name of the resulting file - that is constructed automatically as the name of the directory containing the tree file + the name of the tree file + the number of iterations + the date and time + the index of the current repetition + a random number between 0 and 10000, all separated by underscores.



## Implemented algorithms

The following algorithms are currently included in the project:

**MCTS** - Standard MCTS implementation. Uses UCB1 as its tree policy, random moves as its default policy and picks the move with the highest mean reward at the end.
Class: BasicMCTS.cs
Command string: mcts
Parameters:
- The value of the c parameter used in the UCB1 formula (optional, square root of 2 by default)

**D-UCB MCTS** - An MCTS implementation that uses Discounted UCB (taken from \[1\]) as its tree policy.
Class: DUCBMCTS.cs
Command string: ducbmcts
Parameters:
- The multiplicative factor used in Discounted UCB.
- A boolean indicating whether the "long exploration" variant of the algorithm should be used. If set to true, this means that second half of the formula used as the tree policy will be the same as in UCB1, i.e. it will use the standard, non-discounted visit values.
- The value of the c parameter used in the UCB1 formula (optional, square root of 2 by default)

**D-UCB MCTS 2** - The same as D-UCB MCTS, except it also uses discounted value estimates when picking a move at the end of its run.
Class: DUCBMCTS2.cs
Command string: ducbmcts2
Parameters: The same as D-UCB MCTS.

**SW-UCB MCTS** - An MCTS implementation that uses Sliding window UCB (taken from \[1\]) as its tree policy.
Class: SWUCBMCTS.cs
Command string: swucbmcts
Parameters:
- The size of the window to be used in the tree policy (i.e. 
- A boolean indicating whether the "long exploration" variant of the algorithm should be used. If set to true, this means that second half of the formula used as the tree policy will be the same as in UCB1, i.e. it will use the standard, non-discounted visit values.
- The value of the c parameter used in the UCB1 formula (optional, square root of 2 by default)

**SW-UCB MCTS 2** - The same as SW-UCB MCTS, except it also uses only a given window of observed rewards when picking a move at the end of its turn.
Class: SWUCBMCTS.cs
Command string: swucbmcts
Parameters: The same as SW-UCB MCTS.



## Data visualizer script

This project contains a python file, named data_visualizer.py, for creating plots from the data created by GMC. It contains two methods for this.

**make_plots** - Takes the given algorithms and given data files and creates one plot per data file with data from all algorithms plotted. So, for example, let's say that we used GMC to run experiments with 5 different algorithms - alg_1 to alg_5 - on 100 different trees - tree_1 to tree_100. Now we want to plot the data for all the algorithms, but only for trees 1 through 10. This method allows us to do just that.

arguments:
- Names of the folders containing data for the algorithms to plot.
- Names that should appear in the plot's legend (as these might be different than the names of the folder files).
- Output directory path.
- A boolean that determines whether only specified files should be plotted or all of them. (True <=> only specified ones.)
- If the previous argument is set to True, this specifies names of files that are supposed to be plotted.
- A boolean that determines whether the best choice flag is present in the supplied data (i.e. whether the file contains data about whether the algorithm picked the best move, or just about its convergence).
- The index of data in the file to process (0 <=> data on whether the algorithm would pick the best move, 1 <=> data on whether the algorithm would pick one of the minimax-optimal moves, 2 <=> data on convergence to true minimax/win ratio values).
- A boolean that determines whether variance of the data should be included in the plot.
- A string that determines whether the scale should be linear (lin) or logarithmic (log)
- The upper bound for the x-axis.
- The upper bound for the y-axis
- The path to the directory in which the folder with the algorithm's files is located.

**make_plots_single_alg** - Plots data from files that contain data for a single algorithm, instead of from multiple algorithms. This way, we can plot the data from multiple trees to a single graph.

arguments:
- Name of the folder that contains data for the algorithm we want to plot.
- Names that should appear in the plot's legend.
- Output directory path.
- A boolean that determines whether only specified files should be plotted or all of them. (True <=> only specified ones.)
- If the previous argument is set to True, this specifies names of files that are supposed to be plotted.
- A boolean that determines whether the best choice flag is present in the supplied data (i.e. whether the file contains data about whether the algorithm picked the best move, or just about its convergence).
- The index of data in the file to process (0 <=> data on whether the algorithm would pick the best move, 1 <=> data on whether the algorithm would pick one of the minimax-optimal moves, 2 <=> data on convergence to true minimax/win ratio values).
- A boolean that determines whether variance of the data should be included in the plot.
- A string that determines whether the scale should be linear (lin) or logarithmic (log)
- The upper bound for the x-axis.
- The upper bound for the y-axis
- The path to the directory in which the folder with the algorithm's files is located.



## File formats

### Tree files

The tree files consist mainly of lines specifying the data of different nodes. These have the following format:

{terminal/nonterminal flag}-{the player whose move it currently is}-{whether the current player is maximizing or minimizing}-{the number of points of the first player}-{the number of points of the second player}-{current depth}-,{win ratio},{minimax value}|{position of the node's first child}

These node specifications are separated by two types of separators. '/' is used to separate between children of different nodes and '#' between different levels of the tree.

Below is an example of the first three levels of a tree. At the beginning is the root node, marked N for nonterminal (terminal nodes are marked with T). It is followed by both separators and the position spefied at the end of its specification points to the beginning of the next line (line 4). Lines 4 and 5 specify two children of the root node. These are again followed by separators on lines 6 and 7 and then the specifications of their own children.

N-True-True-5-5-0-,0.6253798498083422,0.5,|00000000059\
/\
\#\
N-False-False-5-4-1-,0.6185157329006215,0.5,|00000000175\
N-False-False-5-4-1-,0.6322439667160629,0,|00000000287\
/\
\#\
N-True-True-4-4-2-,0.5964580768257115,0.5,|00000000556\
N-True-True-5-4-2-,0.6405733889755314,0.5,|00000000837\
/\
N-True-True-4-4-2-,0.4231766999575101,0,|00000000952\
N-True-True-5-4-2-,0.7067996491220235,1,|00000001065\
N-True-True-4-4-2-,0.7028166238110267,1,|00000001399\
N-True-True-4-4-2-,0.5075096721645255,1,|00000001510\
N-True-True-5-4-2-,0.8209171885252289,1,|00000001623\
/\
\#

### Data files

Data files are either the results of running tests, or of crunching multiple pre-existing data files. They contain data measured at every iteration of the algorithm separated by commas. Depending on the test parameters, this can be just data on convergence, i.e. every entry is the difference between the algorithm's estimate of a node's true value (either minimax or win ratio), or it can also contain data about whether the algorithm would pick the best node (the one with the highest minimax value and, if there are more, the one which also maximises the win ratio) and whether it would pick any of the nodes with the highest minimax values. In the latter case, these three pieces of data are separated by vertical bars. An example of a couple of data entries can be seen below. The first number indicates whether the algorithm would pick the best node, the second whether it would pick any of the nodes with the highest minimax value and the third its estimate's deviation from the true minimax value.

0|0|0.6666666666666667,0|0|0.6,1|1|0.5454545454545454,1|1|0.5416666666666667,1|1|0.5384615384615384



## References

[1] A. Garivier and E. Moulines. On upper-confidence bound policies for switching bandit problems. In International conference on algorithmic learning theory, pages 174–188, 2011.
