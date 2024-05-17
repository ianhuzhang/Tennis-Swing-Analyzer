import pandas as pd
from sklearn import metrics
import math


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



def get_start_end_after_classification(data, swing):
    """
    Another preliminary Function to determine start and end indices for a given FHD
    or BHD sensor trace. However this also takes in classification results for
    more accurate results.

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

    for i in range(pos, 0, -1):  
        # Search backwards from moment of highest velocity forwards. Finds moment when z-velocity becomes significant.
        if (
            data["controller_right_vel.z"][i] >= 0.2
            and data["controller_right_vel.z"][i - 1] < 0.2
        ):
            start = i
            break

    if (
        swing != 'VOL'
    ):  
        for i in range(
            neg, len(data["controller_right_vel.z"]) - 1
        ):  # Search forwards from moment of highest velocity backwards. Finds moment when z-velocity becomes insignificant.
            if (
                data["controller_right_vel.z"][i] <= -0.2
                and data["controller_right_vel.z"][i + 1] > -0.2
            ):
                end = i + 1
                break

    else:
        for i in range(
            pos, len(data["controller_right_vel.z"]) - 1
        ):  # Search forwards from moment of highest velocity forwards. Finds moment when z-velocity becomes insignificant.
            if (
                data["controller_right_vel.z"][i] >= 0.2
                and data["controller_right_vel.z"][i + 1] < 0.2
            ):
                end = i + 1
                break

    return (start, end)



def get_rotation(filepath, prediction=None, start_end=None):
    """
    Script to get controller rotation around the body, given a filepath to a 
    CSV sensor trace.

    Takes optional parameters:
        prediction: (str): ("SRV", "FHD", "BHD", "VOL")
        start_end: (tuple(int, int)): start and end of swing
    
    If these optional parameters are not given, they are computed and then used.

    Returns the degrees of rotation that the controller goes through.
    """
    data = pd.read_csv(filepath, index_col=False)

    # When implementing this in actual app, don't re-compute these values!
    # We already have the prediction, and start_end values. Just pass them
    # to this function!!!
    if not prediction:
        prediction = stat_classifier(filepath)
    if not start_end:
        start, end = get_start_end_after_classification(data, prediction)
    else:
        start, end = start_end
    ### 


    res = 0
    for i in range(start, end-2):
        vec1 = (data['controller_right_pos.x'][i] - data['headset_pos.x'][i], data['controller_right_pos.z'][i] - data['headset_pos.z'][i])
        vec2 = (data['controller_right_pos.x'][i+1] - data['headset_pos.x'][i+1], data['controller_right_pos.z'][i+1] - data['headset_pos.z'][i+1])

        dot = vec1[0] * vec2[0] + vec1[1] * vec2[1]
        mag1 = (vec1[0] ** 2 + vec1[1] ** 2) ** 0.5
        mag2 = (vec2[0] ** 2 + vec2[1] ** 2) ** 0.5
        res += math.acos(dot/mag1/mag2) #arc cosine

    return res * 180 / math.pi # WE SHOULD ALSO DECIDE WHAT IS A "GOOD ROTATION" AMOUNT



def get_follow_through(filepath, prediction=None, start_end = None):
    """
    Script to get follow through analytics, given a filepath to a CSV sensor trace.

    Takes optional parameters:
        prediction: (str): ("SRV", "FHD", "BHD", "VOL")
        start_end: (tuple(int, int)): start and end of swing
    
    If these optional parameters are not given, they are computed and then used.
    """
    data = pd.read_csv(filepath, index_col=False)

    if not prediction:
        prediction = stat_classifier(filepath)
    if not start_end:
        _, end = get_start_end_after_classification(data, prediction)
    else:
        _, end = start_end

    tup = (data['controller_right_pos.x'][end] - data['headset_pos.x'][end], data['controller_right_pos.z'][end] - data['headset_pos.z'][end], data['controller_right_pos.y'][end] - data['headset_pos.y'][end])

    if prediction == "FHD":
        success = True
        if tup[2] < -0.2: # Checks height of controller vs. height of headset
            print("Try to follow-through a bit higher, over your shoulder!")
            success = False
        if tup[1] > 0: # Checks "forward depth" of controller vs. headset
            print("Try to end your swing further back!")
            success = False
        if tup[0] > 0: # Checks left/right position of contrller vs. headset
            print("Make sure to complete your follow-through on the left side of your body!")
            success = False
        if success:
            print("Nice follow through!")

    if prediction == "BHD":
        success = True
        if tup[2] < -0.2: # Checks height of controller vs. height of headset
            print("Try to follow-through a bit higher, over your shoulder!")
            success = False
        if tup[1] > 0: # Checks "forward depth" of controller vs. headset
            print("Try to end your swing further back!")
            success = False
        if tup[0] < 0: # Checks left/right position of contrller vs. headset
            print("Make sure to complete your follow-through on the right side of your body!")
            success = False
        if success:
            print("Nice follow through!")

    if prediction == "SRV":
        success = True
        if -1 * tup[2] < max(data["controller_right_pos.y"] - data["headset_pos.y"]):
            # Checks height of controller vs headset, makes sure difference is
            # at least as much as difference during peak of serve
            print("Try to finish a bit lower, near your waist!")
            success = False
        if tup[1] > 0: # Checks "forward depth" of controller vs. headset
            print("Try to end your swing further back!")
            success = False
        if tup[0] < 0: # Checks left/right position of contrller vs. headset
            print("Make sure to complete your follow-through on the left side of your body!")
            success = False
        if success:
            print("Nice follow through!")
    
    if prediction == "VOL":
        success = True
        if tup[2] > 0: # Checks height of controller vs. height of headset
            print("Try to keep your hand below your head in a volley!!")
            success = False
        if tup[1] < 0: # Checks "forward depth" of controller vs. headset
            print("Try to end your volley in front of your body!")
            success = False
        #if tup[0] < 0: # Checks left/right position of contrller vs. headset
        #    print("Make sure to finish on the right side of your body!")
        #    success = False
        if success:
            print("Nice shot!")