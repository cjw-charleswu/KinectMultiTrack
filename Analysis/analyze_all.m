dir_experiments_1_2_3_6 = '../Data/Experiments_1_2_3_6/';
dir_experiments_4 = '../Data/Experiments_4/';

% testing
% filename = strcat(dir_experiments_1_2_3_6,'Study_0_Kinect_0_Time_11_35_09_AM.csv');
% fprintf('Processing file, filename=%s\n',filename);
% valid = validateFile(filename);
% if ~valid
%     exit();
% end

raw_data_table = table();

% loading experiments 1-3 and 6
files = dir(strcat(dir_experiments_1_2_3_6,'*.csv'));
for i=1:length(files)
    file_name = strcat(dir_experiments_1_2_3_6,files(i).name);
    fprintf('Loading file, filename=%s\n',file_name);
    raw_data_table = [raw_data_table;readtable(file_name,'ReadVariableNames',true)];
end
fprintf('All files loaded!!!\n');

disp('Cleaning data...');
tic;
data_table = cleanData(raw_data_table);
time = toc;
fprintf('Done!!!, time=%.2f\n',time);

disp('Creating difference table...');
tic;
[difference_table, difference_joint_types] = getJointDifferenceTable(data_table);
time = toc;
fprintf('Done!!!, time=%.2f\n',time);

disp('Creating time average table...');
tic;
time_average_table = getTimeAverageTable(difference_table, difference_joint_types);
time = toc;
fprintf('Done!!!, time=%.2f\n',time);

disp('Creating joints average table...');
tic;
joints_average_table = getJointsAverageTable(time_average_table);
time = toc;
fprintf('Done!!!, time=%.2f\n',time);

disp('Creating study average table...');
tic;
study_average_table = getStudyAverageTable(joints_average_table);
time = toc;
fprintf('Done!!!, time=%.2f\n',time);
