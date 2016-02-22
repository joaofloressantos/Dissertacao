import java.io.*;
import java.util.Objects;
import java.util.Scanner;
import java.util.regex.Pattern;

/**
 * Created by Jo�o Carlos Santos on 21-Oct-15.
 */

// TODO: Adaptar para receber args por linha de comandos e dar skip ao menu inicial

public class FFMPEGTester {

    public static void main(String[] args) {

        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        String option = "";

        do {
            System.out.println("Main Menu");
            System.out.println("1. Process a video file;");
            System.out.println("2. Quit.\n");
            System.out.print("Choose an option: ");
            try {
                option = reader.readLine();
            } catch (IOException e) {
                e.printStackTrace();
            }

            if (Objects.equals(option, "1")) {
                reader = new BufferedReader(new InputStreamReader(System.in));
                String filePath = "";
                File videoFile;

                // Checking if file exists, and reading its properties

                do {
                    System.out.print("Insert original file path or type exit to go back to main menu: ");
                    try {
                        filePath = reader.readLine();
                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                    if (Objects.equals(filePath, "exit")) {
                        return;
                    }
                    videoFile = new File(filePath);

                } while (!videoFile.exists() || videoFile.isDirectory());

                String fileName = videoFile.getName();
                System.out.println("File exists!");
                System.out.println("File name: " + fileName);
                System.out.println("File type: " + fileName.substring(fileName.lastIndexOf(".") + 1));

                String destinationFolder = filePath.substring(0, filePath.lastIndexOf("."));

                File destinationFolderFile = new File(destinationFolder);

                // if the directory does not exist, create it
                createDestinationFolder(destinationFolder, destinationFolderFile);

                if (divideVideoFile(fileName, filePath, destinationFolder, destinationFolderFile)) {
                    System.out.println("\nDivision completed successfully!\n");
                } else {
                    System.out.println("\nDivision failed!\n");
                    return;
                }

                if (processVideoFile(fileName, filePath, destinationFolder, destinationFolderFile)) {
                    System.out.println("\nProcessing completed successfully!\n");
                } else {
                    System.out.println("\nProcessing failed!\n");
                    return;
                }

                if (mergeVideoFile(fileName, filePath, destinationFolder)) {
                    System.out.println("\nMerging completed successfully!\n");
                } else {
                    System.out.println("\nMerging failed!\n");
                    return;
                }
            }
        } while (!Objects.equals(option, "3"));
    }

    private static void createDestinationFolder(String destinationFolder, File destinationFolderFile) {
        if (!destinationFolderFile.exists()) {
            System.out.println("Creating destination folder: " + destinationFolder);
            boolean result = false;

            try {
                destinationFolderFile.mkdir();
                result = true;
            } catch (SecurityException se) {
                //...
            }
            if (result) {
                System.out.println("Folder created.");
            }
        }
    }

    private static boolean divideVideoFile(String fileName, String filePath, String destinationFolder, File
            destinationFolderFile) {

        // Checking output from ffmpeg -i to find file duration
        String line = "";
        ProcessBuilder builder = new ProcessBuilder("cmd.exe", "/c", "ffmpeg -i " + filePath);
        builder.redirectErrorStream(true);
        Process p = null;
        try {
            p = builder.start();
        } catch (IOException e) {
            e.printStackTrace();
        }
        Scanner sc = new Scanner(p.getInputStream());
        Pattern durationPattern = Pattern.compile("(?<=Duration: )[^,]*");
        String fileDuration = sc.findWithinHorizon(durationPattern, 0);
        String[] hms = fileDuration.split(":");
        double totalSecs = Integer.parseInt(hms[0]) * 3600 + Integer.parseInt(hms[1]) * 60 + Double.parseDouble(hms[2]);
        System.out.println("Total secs: " + totalSecs);

        double secondsPerBlock = 0;

        secondsPerBlock = 0.0;
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        do {
            System.out.println("Insert the time per block (should be less than the number of seconds in the video): ");
            try {
                secondsPerBlock = Double.parseDouble(reader.readLine());
            } catch (IOException e) {
                e.printStackTrace();
            }
        } while (secondsPerBlock >= totalSecs);

        System.out.println("Seconds per block: " + secondsPerBlock);

        // Dividing the files into chunks
        String commandString = "ffmpeg -i " + filePath + " -f segment -segment_time " + secondsPerBlock +
                " -reset_timestamps 1 -c copy " + destinationFolder + "\\" + getFileNameWithoutExtension(fileName) +
                "%03d.mp4";

        builder = new ProcessBuilder("cmd.exe", "/c", commandString);
        builder.redirectErrorStream(true);
        p = null;
        try {
            p = builder.start();
        } catch (IOException e) {
            e.printStackTrace();
        }

        sc = new Scanner(p.getInputStream());
        while (sc.hasNext()) {
            line = sc.nextLine();
            String[] values = line.split("\\s+");
            System.out.println(line);
        }

        // making a file list to use with concat later when merging
        int nFiles = destinationFolderFile.listFiles().length;
        File fileList = new File(destinationFolderFile, "list.txt");
        BufferedWriter writer = null;
        try {
            writer = new BufferedWriter(new FileWriter(fileList));
        } catch (IOException e) {
            e.printStackTrace();
        }

        for (int i = 0; i < nFiles; i++) {
            try {
                writer.write("file '" + destinationFolder + "\\" + getFileNameWithoutExtension(fileName) + String.format
                        ("%03d", i) + ".mp4'");
                writer.newLine();
            } catch (IOException e) {
                e.printStackTrace();
            }
        }
        try {
            writer.flush();
        } catch (IOException e) {
            e.printStackTrace();
        }
        try {
            writer.close();
        } catch (IOException e) {
            e.printStackTrace();
        }

        return true;
    }

    private static boolean processVideoFile(String fileName, String filePath, String destinationFolder, File
            destinationFolderFile) {
        return true;
    }

    private static boolean mergeVideoFile(String fileName, String filePath, String destinationFolder) {
        ProcessBuilder builder;
        Process p;
        Scanner sc;
        String line;
        String commandString = "ffmpeg -f concat -i " + destinationFolder + "\\list.txt -c copy " +
                getParentFolderFromPath(filePath) + "\\" + getFileNameWithoutExtension(fileName) + "pr.mp4";

        System.out.println(commandString);

        builder = new ProcessBuilder("cmd.exe", "/c", commandString);
        builder.redirectErrorStream(true);
        p = null;
        try {
            p = builder.start();
        } catch (IOException e) {
            e.printStackTrace();
        }

        sc = new Scanner(p.getInputStream());
        while (sc.hasNext()) {
            line = sc.nextLine();
            String[] values = line.split("\\s+");
            System.out.println(line);
        }

        return true;
    }


    private static String getFileNameWithoutExtension(String fileName) {
        return fileName.substring(0, fileName.lastIndexOf("."));
    }
    private static String getParentFolderFromPath(String filePath) {
        return filePath.substring(0, filePath.lastIndexOf("\\"));
    }
    private static String forceKeyframes(String filePath, String name, double secondsPerBlock) {
        ProcessBuilder builder;
        Process p;

        builder = new ProcessBuilder("cmd.exe", "/c", "ffmpeg -i " + filePath +
                " -force_key_frames \"expr:gte(t,n_forced*" + secondsPerBlock + ")\" " + filePath.substring(0,
                filePath.lastIndexOf(".")) + "kf" + name.substring(name.lastIndexOf(".")));
        builder.redirectErrorStream(true);
        p = null;
        try {
            p = builder.start();
        } catch (IOException e) {
            e.printStackTrace();
        }
        String line;
        Scanner sc = new Scanner(p.getInputStream());
        while (sc.hasNext()) {
            line = sc.nextLine();
            String[] values = line.split("\\s+");
            System.out.println(line);
        }

        System.out.println("Key frames added to file.");
        return filePath.substring(0, filePath.lastIndexOf(".")) + "kf" + name.substring(name.lastIndexOf("."));
    }
}


