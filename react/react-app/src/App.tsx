import { useState } from "react";
import { Alert } from "./components/Alert";
import { Button } from "./components/Button/Button";
import { Like } from "./components/Like/Like";
import Message from "./Message";
import ListGroup from "./components/ListGroup12";
import "./App.css";
import { BsFillCalendarFill } from "react-icons/bs";

function App() {
  let items = ["New York", "San Francisco", "Tokyo", "London", "Paris"];

  const [alertVisible, setAlertVisibility] = useState(false);

  const handleSelectItem = (item: string) => {
    console.log(item);
  };

  const showAlert = () => {
    setAlertVisibility(true);
  };
  return (
    <div>
      {alertVisible && (
        <Alert onClose={() => setAlertVisibility(false)}>My Alert</Alert>
      )}
      <ListGroup
        items={items}
        heading={"Cities"}
        onSelectItem={handleSelectItem}
      ></ListGroup>
      <Message></Message>
      <div>
        <BsFillCalendarFill color="red" size="40" />
      </div>
      <Button color="primary" onClick={showAlert}>
        My Button
      </Button>
      <br />
      <Like onClick={() => console.log("liked")}></Like>
    </div>
  );
}

export default App;
