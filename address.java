import java.math.BigInteger;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Arrays;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

class Address {
    private static final String ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private static final BigInteger BASE = BigInteger.valueOf(58);

    private static String encode(byte[] input) {
        BigInteger bi = new BigInteger(1, input);
        StringBuffer s = new StringBuffer();
        while (bi.compareTo(BASE) >= 0) {
            BigInteger mod = bi.mod(BASE);
            s.insert(0, ALPHABET.charAt(mod.intValue()));
            bi = bi.subtract(mod).divide(BASE);
        }
        s.insert(0, ALPHABET.charAt(bi.intValue()));
        for (byte anInput : input) {
            if (anInput == 0)
                s.insert(0, ALPHABET.charAt(0));
            else
                break;
        }

        int l = 33 - s.toString().length();

        String result = s.toString();

        for (int i = 0; i < l; i++) {
            result = ALPHABET.charAt(0) + result;
        }

        return result;
    }

    private static byte[] decode(String input) throws Exception {
        if (input.length() == 0) {
            throw new Exception("Attempt to parse an empty address.");
        }
        byte[] bytes = decodeToBigInteger(input).toByteArray();

        boolean stripSignByte = bytes.length > 1 && bytes[0] == 0 && bytes[1] < 0;

        int leadingZeros = 0;
        for (int i = 0; input.charAt(i) == ALPHABET.charAt(0); i++) {
            leadingZeros++;
        }

        byte[] tmp = new byte[bytes.length - (stripSignByte ? 1 : 0) + leadingZeros];
        System.arraycopy(bytes, stripSignByte ? 1 : 0, tmp, leadingZeros, tmp.length - leadingZeros);

        int l = 24 - tmp.length + 1;

        byte[] result = tmp;

        for (int i = 1; i > l; i--) {
            result = Arrays.copyOfRange(result, 1, result.length);
        }

        return result;
    }

    private static BigInteger decodeToBigInteger(String input) throws Exception {
        BigInteger bi = BigInteger.valueOf(0);

        for (int i = input.length() - 1; i >= 0; i--) {
            int alphaIndex = ALPHABET.indexOf(input.charAt(i));
            if (alphaIndex == -1) {
                throw new Exception("Illegal character " + input.charAt(i) + " at " + i);
            }
            bi = bi.add(BigInteger.valueOf(alphaIndex).multiply(BASE.pow(input.length() - 1 - i)));
        }
        return bi;
    }
    
    private static String bytesToHex(byte[] hash) {
        StringBuilder hex_string = new StringBuilder();
        for (byte b : hash) {
            String hex = Integer.toHexString(0xff & b);
            if (hex.length() == 1) hex_string.append('0');
            hex_string.append(hex);
        }
        return hex_string.toString();
    }

    private static byte[] hexToBytes(String s) {
        int len = s.length();
        byte[] data = new byte[len / 2];
        for (int i = 0; i < len; i += 2) {
            data[i / 2] = (byte) ((Character.digit(s.charAt(i), 16) << 4)
                    + Character.digit(s.charAt(i+1), 16));
        }
        return data;
    }

    private static String sha256(String string) throws NoSuchAlgorithmException {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        return bytesToHex(digest.digest(string.getBytes(StandardCharsets.UTF_8)));
    }

    public static String ethToVlx(String address) throws Exception {
        if (address.length() == 0) {
            throw new Exception("Invalid address");
        }

        String eth_prefix = address.substring(0, 2);

        if (!eth_prefix.equals("0x")) {
            throw new Exception("Invalid address");
        }

        String clear_addr = address.substring(2).toLowerCase();
        String checksum = sha256(sha256(clear_addr)).substring(0, 8);

        String long_address = clear_addr + checksum;

        return "V" + encode(hexToBytes(long_address));
    }

    public static String vlxToEth(String address) throws Exception {
        if (address.length() == 0) {
            throw new Exception("Invalid address");
        }

        String vlx_prefix = address.substring(0, 1);

        if (!vlx_prefix.equals("V")) {
            throw new Exception("Invalid address");
        }

        String clear_addr = address.substring(1);
        String decode_addr = bytesToHex(decode(clear_addr));

        Pattern pattern = Pattern.compile("([0-9abcdef]+)([0-9abcdef]{8})");
        Matcher matcher = pattern.matcher(decode_addr);

        if (matcher.find()) {
            if (matcher.groupCount() != 2) {
                throw new Exception("Invalid address");
            }

            String checksum = sha256(sha256(matcher.group(1))).substring(0, 8);

            if (!matcher.group(2).equals(checksum)) {
                throw new Exception("Invalid checksum");
            }

            String new_address =  "0x" + matcher.group(1);

            if (new_address.length() != 42) {
                throw new Exception("Invalid address");
            }

            return new_address;
        } else {
            throw new Exception("Invalid address");
        }
    }

    public static void main(String[] args) throws Exception {
        String[] ethAddresses = {
            "0x32Be343B94f860124dC4fEe278FDCBD38C102D88",
            "0x000000000000000000000000000000000000000f",
            "0xf000000000000000000000000000000000000000",
            "0x0000000000000000000000000000000000000001",
            "0x1000000000000000000000000000000000000000",
            "0x0000000000000000000000000000000000000000",
            "0xffffffffffffffffffffffffffffffffffffffff"
        };

        String[] vlxAddresses = {
            "V5dJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f",
            "V111111111111111111111111112jSS6vy",
            "VNt1B3HD3MghPihCxhwMxNKRerBPPbiwvZ",
            "V111111111111111111111111111CdXjnE",
            "V2Tbp525fpnBRiSt4iPxXkxMyf5ZX7bGAJ",
            "V1111111111111111111111111113iMDfC",
            "VQLbz7JHiBTspS962RLKV8GndWFwdcRndD"
        };
        
        for (int i = 0; i < ethAddresses.length; i++) {
            System.out.println(ethToVlx(ethAddresses[i]));
        }

        for (int i = 0; i < vlxAddresses.length; i++) {
            System.out.println(vlxToEth(vlxAddresses[i]));
        }

        for (int i = 0; i < ethAddresses.length; i++) {
            String addr = ethAddresses[i];
            String eth_addr = vlxToEth(ethToVlx(addr));
            System.out.println(eth_addr.equals(addr.toLowerCase()));
        }
    }
}