import { useState } from "react";
import styles from "./Like.module.css";
import { AiFillHeart, AiOutlineHeart } from "react-icons/ai";

interface Props {
  onClick: () => void;
}

export const Like = ({ onClick }: Props) => {
  const [liked, setLiked] = useState(false);

  const toggle = () => {
    setLiked(!liked);
    onClick();
  };
  if (liked)
    return (
      <AiFillHeart className={styles.likeLiked} size={20} onClick={toggle} />
    );
  return <AiOutlineHeart size="20" onClick={toggle} />;
};
