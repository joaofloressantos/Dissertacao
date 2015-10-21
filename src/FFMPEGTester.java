import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.Objects;
import java.util.Scanner;
import java.util.regex.Pattern;

/**
 * Created by João Carlos Santos on 21-Oct-15.
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
        String option = "";
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
        System.out.println(line);
        Pattern durationPattern = Pattern.compile("(?<=Duration: )[^,]*");
        String fileDuration = sc.findWithinHorizon(durationPattern, 0);
        String[] hms = fileDuration.split(":");
        double totalSecs = Integer.parseInt(hms[0]) * 3600 + Integer.parseInt(hms[1]) * 60 + Double.parseDouble(hms[2]);
        System.out.println("Total secs: " + totalSecs);

        do {
            System.out.println("Choose file division type or type exit to go back to the main menu: ");
            System.out.println("1. By number of blocks;");
            System.out.println("2. By block duration.");
            System.out.print("Choose an option: ");
            try {
                option = reader.readLine();
            } catch (IOException e) {
                e.printStackTrace();
            }
            System.out.println("option: " + option);
        } while (!Objects.equals(option, "1") && !Objects.equals(option, "2") && !Objects.equals(option, "exit"));

        switch (option) {
            case "1":
                Integer option2 = 0;
                do {
                    System.out.println("Insert the number of desired blocks (should be less or equal to the number of "
                            + "seconds in the video): ");
                    try {
                        option2 = Integer.parseInt(reader.readLine());
                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                }while(option2>totalSecs || option2<2);
            case "2":
                double option3 = 0.0;
                do {
                    System.out.println("Insert the time per block (should be less than the number of "
                            + "seconds in the video): ");
                    try {
                        option3 = Double.parseDouble(reader.readLine());
                    } catch (IOException e) {
                        e.printStackTrace();
                    }
                }while(option3>=totalSecs || option3<2);
            case "exit":
                return false;
        }

        return true;
    }

}


