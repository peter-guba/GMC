# Creates line plots from data produced by the Cruncher part of GMC.

import seaborn as sns
import numpy as np
import matplotlib.pyplot as plt
import pandas as pd
import os


# Creates plots containing data from the supplied algorithms.
def make_plots(
        algs,               # Names of the folders containing data for the algorithms to plot
        names,              # Names that should appear in the plot's legend
        results_dir,        # Output directory
        only_selected,      # Determines whether only specified files should be plotted or all of them
        selected,           # If only_selected is set to true, this specifies the names of files that are supposed to be plotted
        bcf_present,        # Determines whether the best choice flag is present (i.e. whether the file contains data about whether the algorithm picked the best move)
        data_index,         # The index of data to process (0 <=> whether the algorithm would pick the best move, 1 <=> whether the algorithm would pick one of the minimax-optimal moves, 2 <=> convergence to true minimax/win ratio values)
        include_variance,   # Determines whether variance should be included
        scale,              # Determines whether the scale should be linear (lin) or logarithmic (log)
        x_ub,               # The upper bound for the x-axis
        y_ub,               # The upper bound for the y-axis
        base_dir            # The path to the directory in which the folder with the algorithm's files is located
):
    alg_dirs = [base_dir + "/" + a for a in algs]

    # If only files that contain crunched data for whole categories are supposed to be
    # visualized...
    if only_selected:
        alg_files = [selected for _ in alg_dirs]
    else:
        # Get all the files for every algorithm except for those that contain variance data.
        alg_files = [[f for f in os.listdir(a) if not f.endswith("var.txt")] for a in alg_dirs]

    # Check if every algorithm has the same number of associated files.
    for afs in alg_files:
        if len(afs) != len(alg_files[0]):
            raise Exception("That won't fit.")

    if not os.path.isdir(results_dir):
        os.makedirs(results_dir)

    # A list of first lines to be passed to the pyplot as those that
    # are supposed to be shown in the legend.
    lines = []

    for file_index in range(len(alg_files[0])):
        file_name = alg_files[0][file_index]
        for afs in alg_files:
            if file_name != afs[file_index]:
                raise Exception("No harmony.")

        data_conv = []
        vars_data_conv = []
        dfs = []

        # Load the convergence data and create appropriate dataframes.
        for afs_index in range(len(alg_files)):
            if not os.path.isfile(alg_dirs[afs_index] + '/' + file_name):
                print(alg_dirs[afs_index] + '/' + file_name)
                raise Exception("why must you hurt me this way?")

            if bcf_present:

                conv = {}
                for i in range(x_ub):
                    conv[i] = lambda x: float(x.split(b'|')[data_index])

                data_conv.append(
                    np.loadtxt(
                        alg_dirs[afs_index] + '/' + file_name,
                        delimiter=",",
                        converters=conv,
                        usecols=[x for x in range(x_ub)],
                        dtype=float
                    )
                )

                if include_variance:
                    vars_data_conv.append(
                        np.loadtxt(
                            alg_dirs[afs_index] + '/' + file_name[:-4] + "_var.txt",
                            delimiter=",",
                            converters=conv,
                            usecols=[x for x in range(x_ub)],
                            dtype=float
                        )
                    )
                else:
                    vars_data_conv.append([0]*len(data_conv[afs_index]))

            else:
                if data_index != 0:
                    raise Exception("Only supposed to process convergence data.")

                data_conv.append(
                    np.loadtxt(
                        alg_dirs[afs_index] + '/' + file_name,
                        delimiter=",",
                        usecols=[x for x in range(x_ub)],
                        dtype=float
                    )
                )

                if include_variance:
                    vars_data_conv.append(
                        np.loadtxt(
                            alg_dirs[afs_index] + '/' + file_name[:-4] + "_var.txt",
                            delimiter=",",
                            usecols=[x for x in range(x_ub)],
                            dtype=float
                        )
                    )
                else:
                    vars_data_conv.append([0]*len(data_conv[afs_index]))

            dfs.append(
                pd.DataFrame(
                    {
                        "data": data_conv[afs_index],
                        "vars": vars_data_conv[afs_index]
                    },
                    index=[x for x in range(1, len(data_conv[afs_index]) + 1)]
                )
            )

        fig, ax = plt.subplots()

        for df_index in range(len(dfs)):
            sns.lineplot(dfs[df_index]["data"], ax=ax)
            lines.append(ax.lines[-1])

            if include_variance:
                ax.fill_between(
                    np.linspace(1, 10000, 10000),
                    dfs[df_index]["data"] - dfs[df_index]["vars"],
                    dfs[df_index]["data"] + dfs[df_index]["vars"],
                    alpha=0.2
                )

        ax.set_xlabel("")
        ax.set_ylabel("")
        ax.set_xlim(1, x_ub)
        ax.set_ylim(0, y_ub)
        plt.legend(lines, names)
        plt.xscale(scale)
        plt.grid()
        plt.savefig(results_dir + '/' + file_name[file_name.find('_') + 1:file_name.rfind('.')] + ".png")
        plt.close()


# Plots different data from a single algorithm to a single image
# (e.g. data from different size categories).
def make_plots_single_alg(
        alg,                # Algorithm folder name
        names,              # Names that should appear in the plot's legend
        results_dir,        # Output directory
        only_selected,      # Determines whether only specified files should be plotted or all of them
        selected,           # If only_selected is set to true, this specifies the names of files that are supposed to be plotted
        bcf_present,        # Determines whether the best choice flag is present (i.e. whether the file contains data about whether the algorithm picked the best move)
        data_index,         # The index of data to process (0 <=> whether the algorithm would pick the best move, 1 <=> whether the algorithm would pick one of the minimax-optimal moves, 2 <=> convergence to true minimax/win ratio values)
        include_variance,   # Determines whether variance should be included
        scale,              # Determines whether the scale should be linear (lin) or logarithmic (log)
        x_ub,               # The upper bound for the x-axis
        y_ub,               # The upper bound for the y-axis
        base_dir            # The path to the directory in which the folder with the algorithm's files is located
):
    alg_dir = base_dir + "/" + alg

    # If only files that contain crunched data for whole categories are supposed to be
    # visualized...
    if only_selected:
        alg_files = selected
    else:
        # Get all the files for in the algorithm folder.
        alg_files = [f for f in os.listdir(alg_dir) if not f.endswith("var.txt")]

    if not os.path.isdir(results_dir):
        os.makedirs(results_dir)

    # A list of first lines to be passed to the pyplot as those that
    # are supposed to be shown in the legend.
    lines = []

    data_conv = []
    vars_data_conv = []
    dfs = []

    # Load the convergence data and create appropriate dataframes.
    for file_index in range(len(alg_files)):
        file_name = alg_files[file_index]
        if not os.path.isfile(alg_dir + '/' + file_name):
            print(alg_dir + '/' + file_name)
            raise Exception("why must you hurt me this way?")

        if bcf_present:

            conv = {}
            for i in range(x_ub):
                conv[i] = lambda x: float(x.split(b'|')[data_index])

            data_conv.append(
                np.loadtxt(
                    alg_dir + '/' + file_name,
                    delimiter=",",
                    converters=conv,
                    usecols=[x for x in range(x_ub)],
                    dtype=float
                )
            )

            if include_variance:
                vars_data_conv.append(
                    np.loadtxt(
                        alg_dir + '/' + file_name[:-4] + "_var.txt",
                        delimiter=",",
                        converters=conv,
                        usecols=[x for x in range(x_ub)],
                        dtype=float
                    )
                )
            else:
                vars_data_conv.append([0] * len(data_conv[file_index]))

        else:
            if data_index != 0:
                raise Exception("Only supposed to process convergence data.")

            data_conv.append(
                np.loadtxt(
                    alg_dir + '/' + file_name,
                    delimiter=",",
                    usecols=[x for x in range(x_ub)],
                    dtype=float
                )
            )

            if include_variance:
                vars_data_conv.append(
                    np.loadtxt(
                        alg_dir + '/' + file_name[:-4] + "_var.txt",
                        delimiter=",",
                        usecols=[x for x in range(x_ub)],
                        dtype=float
                    )
                )
            else:
                vars_data_conv.append([0] * len(data_conv[file_index]))

        dfs.append(
            pd.DataFrame(
                {
                    "data": data_conv[file_index],
                    "vars": vars_data_conv[file_index]
                },
                index=[x for x in range(1, len(data_conv[file_index]) + 1)]
            )
        )

    fig, ax = plt.subplots()

    for df_index in range(len(dfs)):
        sns.lineplot(dfs[df_index]["data"], ax=ax)
        lines.append(ax.lines[-1])

        if include_variance:
            ax.fill_between(
                np.linspace(1, 10000, 10000),
                dfs[df_index]["data"] - dfs[df_index]["vars"],
                dfs[df_index]["data"] + dfs[df_index]["vars"],
                alpha=0.2
            )

    ax.set_xlabel("")
    ax.set_ylabel("")
    ax.set_xlim(1, x_ub)
    ax.set_ylim(0, y_ub)
    plt.legend(lines, names)
    plt.xscale(scale)
    plt.grid()
    plt.savefig(results_dir + '/' + file_name[file_name.find('_') + 1:file_name.rfind('.')] + ".png")
    plt.close()
