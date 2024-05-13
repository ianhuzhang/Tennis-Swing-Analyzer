import pandas as pd
from sklearn import metrics


def get_start_end(data):
    """
    Another preliminary Function to determine start and end indices for a given FHD
    or BHD sensor trace.

    Returns tuple, of indices at which start and end of swing are estimated
    to be.
    """
    start = 0
    end = len(data["controller_right_vel.z"])

    data["controller_right_vel"] = (
        data["controller_right_vel.x"] ** 2
        + data["controller_right_vel.y"] ** 2
        + data["controller_right_vel.z"] ** 2
    ) ** (1 / 2)

    pos = -1
    neg = -1
    min = float("inf")
    max = -float("inf")
    apex = -1
    max_apex = -float("inf")

    for i in range(
        len(data["controller_right_vel"])
    ):  # Finding moment of highest Velocity
        if float(data["controller_right_vel"][i]) > max_apex:
            apex = i
            max_apex = data["controller_right_vel"][i]

    for i in range(apex):
        if (
            float(data["controller_right_vel.z"][i]) > max
        ):  # Finding moment of highest velocity forwards, up to apex
            max = float(data["controller_right_vel.z"][i])
            pos = i
    for i in range(
        apex, len(data["controller_right_vel.z"])
    ):  # Finding moment of highest velocity backwards, after apex
        if float(data["controller_right_vel.z"][i]) < min:
            min = float(data["controller_right_vel.z"][i])
            neg = i

    if (
        min > -1
    ):  # If no significant velocity backwards, start both searches at highest velocity forwards
        neg = pos  # This assumes your swing moves forwards. If it doesn't then you might be hopeless

    for i in range(
        pos, 0, -1
    ):  # Search backwards from moment of highest velocity forwards. Finds moment when z-velocity becomes significant.
        if (
            data["controller_right_vel.z"][i] >= 0.2
            and data["controller_right_vel.z"][i - 1] < 0.2
        ):
            start = i
            break

    for i in range(
        neg, len(data["controller_right_vel.z"]) - 1
    ):  # Search forwards from moment of highest velocity backwards (or forwards, if backward movement is not found). Finds moment when z-velocity becomes insignificant.
        if (
            abs(data["controller_right_vel.z"][i]) >= 0.2
            and abs(data["controller_right_vel.z"][i + 1]) < 0.2
        ):
            end = i + 1
            break

    return (start, end)


def get_start_end2(data):
    """
    Another preliminary Function to determine start and end indices for a given FHD
    or BHD sensor trace.

    Returns tuple, of indices at which start and end of swing are estimated
    to be.
    """
    start = 0
    end = len(data["controller_right_vel.z"])

    data["controller_right_vel"] = (
        data["controller_right_vel.x"] ** 2
        + data["controller_right_vel.y"] ** 2
        + data["controller_right_vel.z"] ** 2
    ) ** (1 / 2)

    pos = -1
    max = -float("inf")
    apex = -1
    max_apex = -float("inf")

    for i in range(
        len(data["controller_right_vel"])
    ):  # Finding moment of highest Velocity
        if float(data["controller_right_vel"][i]) > max_apex:
            apex = i
            max_apex = data["controller_right_vel"][i]

    for i in range(apex):
        if (
            float(data["controller_right_vel.z"][i]) > max
        ):  # Finding moment of highest velocity forwards, up to apex
            max = float(data["controller_right_vel.z"][i])
            pos = i

    for i in range(pos, 0, -1):
        # Search backwards from moment of highest velocity forwards. Finds moment when z-velocity becomes significant.
        # start
        if (
            abs(data["controller_right_vel.z"][i]) >= 0.2
            and abs(data["controller_right_vel.z"][i - 1]) < 0.2
        ):
            start = i
            break
        # end
    for i in range(start, len(data["controller_right_vel"])):
        if (
            abs(data["controller_right_vel.z"][i]) <= 0.2
            and abs(data["controller_right_vel.z"][i - 1]) > 0.2
            and (
                abs(data["controller_right_vel.z"][i - 1])
                > abs(data["controller_right_vel.x"][i - 1])
            )
        ):
            end = i
            break
        if i == len(data["controller_right_vel"]) - 1:
            end = i
            break
    return start, end


def get_start(data):
    """
    Another preliminary Function to determine start index for a given FHD
    or BHD sensor trace.

    Returns integer, of index at which start of swing is estimated to be.
    """
    start = 0

    data["controller_right_vel"] = (
        data["controller_right_vel.x"] ** 2
        + data["controller_right_vel.y"] ** 2
        + data["controller_right_vel.z"] ** 2
    ) ** (1 / 2)

    pos = -1
    max = -float("inf")
    apex = -1
    max_apex = -float("inf")

    for i in range(
        len(data["controller_right_vel"])
    ):  # Finding moment of highest Velocity
        if float(data["controller_right_vel"][i]) > max_apex:
            apex = i
            max_apex = data["controller_right_vel"][i]

    for i in range(apex):
        if (
            float(data["controller_right_vel.z"][i]) > max
        ):  # Finding moment of highest velocity forwards, up to apex
            max = float(data["controller_right_vel.z"][i])
            pos = i

    for i in range(
        pos, 0, -1
    ):  # Search backwards from moment of highest velocity forwards. Finds moment when z-velocity becomes significant.
        if (
            data["controller_right_vel.z"][i] >= 0.2
            and data["controller_right_vel.z"][i - 1] < 0.2
        ):
            start = i
            break

    return start


def stat_classifier(filepath):
    """
    Takes a filepath containing a sensor trace. First trims data according to
    isolate the swing action. Then, based on trimmed csv file, runs statistical
    classifier to determine swing type.

    Returns string, determining guessed swing type.
    """
    data = pd.read_csv(filepath, index_col=False)
    data["controller_right_vel"] = (
        data["controller_right_vel.x"] ** 2
        + data["controller_right_vel.y"] ** 2
        + data["controller_right_vel.z"] ** 2
    ) ** (1 / 2)

    start, end = get_start_end(data)  # Determine start and end of swing
    data = data[start:end]

    idx_max = -1
    mx = -float("inf")  # Finding moment of maximum velocity
    for i in range(len(data["controller_right_vel"])):
        i += start
        if float(data["controller_right_vel"][i]) > mx:
            mx = float(data["controller_right_vel"][i])
            idx_max = i

    if float(data["controller_right_vel.y"][idx_max]) > 0:  # If swing moves upwards
        # BHD or FHD
        if (
            float(data["controller_right_vel.x"][idx_max]) < 0
        ):  # If swing moves left/right
            return "FHD"
        else:
            return "BHD"
    else:
        # SRV or VOL    # Finding peak of controller, relative to headset
        if (
            max((data["controller_right_pos.y"] - data["headset_pos.y"])) > 0.2
        ):  # 0.2 may need to be normalized
            # if max((data['controller_right_pos.y'] - data['headset_pos.y'])) > 0: # This works for Ian/Lucien, but gets Jason wrong every single time lol
            return "SRV"
        else:
            return "VOL"


def test_accuracy(classifier, errors=False):
    """
    Function to test how accurate your classifier is. Takes a classifier
    (a function) that takes a filepath, and returns a string, either
    'BHD', 'FHD', 'VOL', or 'SRV'.

    Also can specifiy whether to print errors.

    Returns accuracy score of the classifier.
    """
    actual = []
    predicted = []
    csv_number = []
    for action in ["BHD", "FHD", "VOL", "SRV"]:
        for i in range(1, 91):
            filepath = "../Data/Combined/" + action + "_" + str(i).zfill(2) + ".csv"
            actual.append(action)
            predicted.append(classifier(filepath))
            csv_number.append(i)

    if errors:
        for i in range(len(actual)):
            if actual[i] != predicted[i]:
                print(actual[i], predicted[i], csv_number[i])

    return metrics.accuracy_score(actual, predicted)


def add_features(data):
    """
    Takes a raw sensor trace as a dataframe

    Adds four features to the data:
     - Controller right velocity
     - Controller right velocity.x at max velocity
     - Controller right velocity.y at max velocity
     - Controller right pos.y relative to headset
    
    Returns the same dataframe, with the four new features.
    """
    
    data['controller_right_vel'] = (data['controller_right_vel.x'] ** 2 + data['controller_right_vel.y'] ** 2 + data['controller_right_vel.z'] ** 2) ** (1/2)


    idx_max = -1
    mx = -float('inf') # Finding moment of maximum velocity
    for i in range(len(data['controller_right_vel'])):
        if float(data['controller_right_vel'][i]) > mx:
            mx = float(data['controller_right_vel'][i])
            idx_max = i
    
    data['controller_right_vel.x_at_max_vel'] = data['controller_right_vel.x'][idx_max]
    data['controller_right_vel.y_at_max_vel'] = data['controller_right_vel.y'][idx_max]
    data['controller_right_pos.y_rel_headset'] = data['controller_right_pos.y'] - data['headset_pos.y']

    return data
