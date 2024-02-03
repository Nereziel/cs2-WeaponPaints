<?php
/**
 * Class DataBase
 * 
 * This class handles database operations using PDO.
 */
class DataBase {
    /**
     * @var PDO The PDO instance for database connection.
     */
    private $PDO;

    /**
     * Constructor method to initialize the database connection.
     */
    public function __construct() {
        try {
            // Establish a connection to the database using PDO
            $this->PDO = new PDO(
                "mysql:host=".DB_HOST."; port=".DB_PORT."; dbname=".DB_NAME, 
                DB_USER, 
                DB_PASS, 
                array(PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES utf8")
            );
        }
        catch(PDOException $ex) {
            // Display error message if connection fails
            echo "<div style='display: flex; flex-direction: column;align-items: center;justify-content: center;text-align: center;'><h2>Problem with database!</h2>";
            die("<pre style='padding: 10px;text-wrap: balance; border: 2px solid #ed6bd3;background: #252525; color: #ed6bd3; width: 50%;'>" . $ex . "</pre>");
        }
    }

    /**
     * Perform a SELECT query on the database.
     *
     * @param string $query The SQL query to execute.
     * @param array $bindings An associative array of parameters and their values.
     * @return array|false Returns an array of rows as associative arrays or false if no results are found.
     */
    public function select($query, $bindings = []) {
        // Prepare and execute the SQL query
        $STH = $this->PDO->prepare($query);
        $STH->execute($bindings);

        // Fetch the results as associative arrays
        $result = $STH->fetchAll(PDO::FETCH_ASSOC);
        $result ??= false; // Set $result to false if it's null
        return $result;
    }

    /**
     * Perform a non-query SQL statement on the database.
     *
     * @param string $query The SQL query to execute.
     * @param array $bindings An associative array of parameters and their values.
     * @return bool Returns true on success or false on failure.
     */
    public function query($query, $bindings = []) {
        // Prepare and execute the SQL query
        $STH = $this->PDO->prepare($query);
        return $STH->execute($bindings);
    }
}
