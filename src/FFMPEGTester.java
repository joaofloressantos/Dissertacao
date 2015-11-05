import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Objects;
import java.util.Scanner;
import java.util.regex.Pattern;

/**
 * Created by Jo�o Carlos Santos on 21-Oct-15.
 */
public class FFMPEGTester {

    public static void main(String[] args) {

	/*TODO: Menu 1 with
    * 1. Choose original file path DONE
	* 2. Choose kind of division between number of block or time per block
	* 3. Choose output location and name
	*/

	/*TODO: Menu 2 with
    * 1. Choose blocks' folder
	* 2. Choose output location and name
	*/
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        String option = "";

        do {
            System.out.println("Main Menu");
            System.out.println("1. Divide a video file;");
            System.out.println("2. Merge a video file;");
            System.out.println("3. Quit.\n");
            System.out.print("Choose an option: ");
            try {
                option = reader.readLine();
            } catch (IOException e) {
                e.printStackTrace();
            }

            switch (option) {
                case "1":
                    if (divideVideoFile()) {
                        System.out.println("\nOperation completed successfully!\n");
                    } else {
                        System.out.println("\nOperation failed!\n");
                    }
                    break;
                case "2":
                    if (mergeVideoFile()) {
                        System.out.println("\nOperation completed successfully!\n");
                    } else {
                        System.out.println("\nOperation failed!\n");
                    }
                    break;
            }

        } while (!Objects.equals(option, "3"));
    }

    private static boolean mergeVideoFile() {

        return true;
    }

    private static boolean divideVideoFile() {
        BufferedReader reader = new BufferedReader(new InputStreamReader(System.in));
        String filePath = "";
        File videoFile;
        do {
            System.out.print("Insert original file path or type exit to go back to main menu: ");
            try {
                filePath = reader.readLine();
            } catch (IOException e) {
                e.printStackTrace();
            }
            if (Objects.equals(filePath, "exit")) {
                return false;
            }
            videoFile = new File(filePath);

        } while (!videoFile.exists() || videoFile.isDirectory());

        // Checking if file exists, and reading its properties
        String fileName = videoFile.getName();
        System.out.println("File exists!");
        System.out.println("File name: " + fileName);
        System.out.println("File type: " + fileName.substring(fileName.lastIndexOf(".") + 1));

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

        double lastBlock, secondsPerBlock, numberOfBlocks = 0;
        int unevenLast = 0;

        secondsPerBlock = 0.0;
        do {
            System.out.println("Insert the time per block (should be less than the number of seconds in the video): ");
            try {
                secondsPerBlock = Double.parseDouble(reader.readLine());
            } catch (IOException e) {
                e.printStackTrace();
            }
        } while (secondsPerBlock >= totalSecs);

        System.out.println("Seconds per block: " + secondsPerBlock);
        numberOfBlocks = Math.floor(totalSecs / secondsPerBlock);
        System.out.println("Number of blocks: " + numberOfBlocks);
        lastBlock = Math.round((totalSecs % numberOfBlocks) * 100.0) / 100.0;

        if (lastBlock > 0) {
            System.out.println("Duration of last block: " + lastBlock);
            unevenLast = 1;
            numberOfBlocks++;
        }

        // force keyframes at determined interval
        String filePathKf = forceKeyframes(filePath, videoFile.getName(), secondsPerBlock);
        System.out.println("New file path: " + filePathKf);

        //TODO: creation of split files
        System.out.println("Separating file...");
        String destinationFolder = filePath.substring(0, filePath.lastIndexOf("."));

        File destinationFolderFile = new File(destinationFolder);

        // if the directory does not exist, create it
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

        SimpleDateFormat df = new SimpleDateFormat("HH:mm:ss");
        Date d = null;
        try {
            d = df.parse("00:00:00");
        } catch (ParseException e) {
            e.printStackTrace();
        }
        Long time = d.getTime();
        Long dtime = d.getTime();
        String commandString;

        for (int i = 0; i < numberOfBlocks; i++) {
            if (i == numberOfBlocks - 1) {
                dtime = dtime + (long) (lastBlock * 1000);
                commandString = "ffmpeg -ss " + new SimpleDateFormat("HH:mm:ss.SSSS").format(time) +
                        " -to " + new SimpleDateFormat("HH:mm:ss.SSSS").format(dtime) + " -i " + filePathKf + " " +
                        "-vcodec copy -acodec copy " + destinationFolder + "\\" + videoFile.getName().substring(0,
                        videoFile.getName().lastIndexOf("" + ".")) + i + videoFile.getName().substring(videoFile
                        .getName().lastIndexOf("."));
            } else {
                dtime = dtime + (long) (secondsPerBlock * 1000);
                commandString = "ffmpeg -ss " + new SimpleDateFormat("HH:mm:ss.SSSS").format(time) +
                        " -to " + new SimpleDateFormat("HH:mm:ss.SSSS").format(dtime) + " -i " + filePathKf + " " +
                        "-vcodec copy -acodec copy " + destinationFolder + "\\" + videoFile.getName().substring(0,
                        videoFile.getName().lastIndexOf("" + ".")) + i + videoFile.getName().substring(videoFile
                        .getName().lastIndexOf("."));
                time = time + (long) (secondsPerBlock * 1000);
            }

            System.out.println("");
            System.out.println("");
            System.out.println("");
            System.out.println("");
            System.out.println(commandString);
            System.out.println("");
            System.out.println("");
            System.out.println("");
            System.out.println("");

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
        }

        return true;
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


