import pandas as pd

def get_start_end(data):
    """
    Another preliminary Function to determine start and end indices for a given FHD
    or BHD sensor trace.

    Returns tuple, of indices at which start and end of swing are estimated
    to be.
    """
    start = 0
    end = len(data['controller_right_vel.z'])

    data['controller_right_vel'] = (data['controller_right_vel.x'] ** 2 + data['controller_right_vel.y'] ** 2 + data['controller_right_vel.z'] ** 2) ** (1/2)

    pos = -1
    neg = -1
    min = float('inf')
    max = -float('inf')
    apex = -1
    max_apex = -float('inf')

    for i in range(len(data['controller_right_vel'])): # Finding moment of highest Velocity
        if float(data['controller_right_vel'][i]) > max_apex:
            apex = i
            max_apex = data['controller_right_vel'][i]

    for i in range(apex):
        if float(data['controller_right_vel.z'][i]) > max: # Finding moment of highest velocity forwards, up to apex
            max = float(data['controller_right_vel.z'][i])
            pos = i
    for i in range(apex, len(data['controller_right_vel.z'])): # Finding moment of highest velocity backwards, after apex
        if float(data['controller_right_vel.z'][i]) < min:
            min = float(data['controller_right_vel.z'][i])
            neg = i

    if min > -1: # If no significant velocity backwards, start both searches at highest velocity forwards
        neg = pos # This assumes your swing moves forwards. If it doesn't then you might be hopeless

    for i in range(pos, 0, -1): # Search backwards from moment of highest velocity forwards. Finds moment when z-velocity becomes significant.
        if data['controller_right_vel.z'][i] >= 0.2 and data['controller_right_vel.z'][i-1] < 0.2:
            start = i
            break

    for i in range(neg, len(data['controller_right_vel.z'])-1): # Search forwards from moment of highest velocity backwards (or forwards, if backward movement is not found). Finds moment when z-velocity becomes insignificant.
        if abs(data['controller_right_vel.z'][i]) >= 0.2 and abs(data['controller_right_vel.z'][i+1]) < 0.2:
            end = i+1
            break
    
    return (start, end)